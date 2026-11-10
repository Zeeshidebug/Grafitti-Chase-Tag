using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class MovementCameraTests
{
    private const string SceneSetupKey = "MovementCameraTests.SceneSetup";
    [System.Serializable]
    private class SavedScenes { public SceneSetup[] scenes; }

    private Keyboard keyboard;
    private Mouse mouse;
    private InputSettings originalInputSettings;
    private float previousCaptureDeltaTime;

    [SetUp]
    public void PreserveOpenScenes()
    {
        // EditMode UnityTests rerun SetUp after EnterPlayMode's domain reload.
        if (Application.isPlaying || !string.IsNullOrEmpty(SessionState.GetString(SceneSetupKey, "")))
            return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
                Assert.Ignore("Save open scenes before running camera verification; unsaved edits are preserved.");
        }
        SessionState.SetString(SceneSetupKey, JsonUtility.ToJson(new SavedScenes
        {
            scenes = EditorSceneManager.GetSceneManagerSetup()
        }));
    }

    [TestCase("Gameplay")]
    [TestCase("AI-NAV-TEST2")]
    [TestCase("AI-Nav-Test")]
    [TestCase("SpawnPoint-AITagging-test")]
    public void ScenesHaveOneIndependentLookOwner(string name)
    {
        EditorSceneManager.OpenScene($"Assets/Project/Scenes/{name}.unity");
        var camera = Object.FindFirstObjectByType<CinemachineCamera>();
        var orbit = camera.GetComponent<CinemachineOrbitalFollow>();
        Assert.That(orbit, Is.TypeOf<TraversalOrbitalFollow>());
        Assert.That(orbit.TrackerSettings.BindingMode, Is.EqualTo(BindingMode.WorldSpace));
        Assert.That(orbit.HorizontalAxis.Recentering.Enabled, Is.False);
        // Vertical recentering is an authored framing preference, independent of player yaw.
        Assert.That(camera.GetComponent<CinemachinePanTilt>(), Is.Null);
        Assert.That(camera.GetComponent<CinemachineRotationComposer>(), Is.Not.Null);
        Assert.That(camera.GetComponent<MovementCameraFeedback>(), Is.Not.Null);
        Assert.That(camera.GetComponent<TraversalDeoccluder>(), Is.Not.Null);
        Assert.That(camera.GetComponent<CinemachineDeoccluder>().CollideAgainst.value & (1 << 10), Is.Zero);
        var inputs = camera.GetComponent<CinemachineInputAxisController>().Controllers;
        var look = InputSystem.actions.FindAction("Player/Look", throwIfNotFound: true);
        var lookAxes = inputs.Where(x => x.Enabled && x.Input.InputAction?.action == look);
        Assert.That(lookAxes.Select(x => x.Name), Is.EquivalentTo(new[] { "Look Orbit X", "Look Orbit Y" }));
        Assert.That(inputs.Where(x => x.Enabled).All(x => x.Owner == orbit), Is.True);
        Assert.That(camera.Follow.GetComponent<PlayerMovementIntent>(), Is.Not.Null);
    }

    private static void PrepareArena(string sceneName = "Gameplay", bool includePole = false)
    {
        // Clone the actual authored player/camera into an isolated physics arena.
        // This exercises real input, movement, stamina, slide, Cinemachine and collision.
        var sourceScene = EditorSceneManager.OpenScene($"Assets/Project/Scenes/{sceneName}.unity");
        var sourcePlayer = Object.FindFirstObjectByType<PlayerMovementIntent>();
        var sourceCamera = Object.FindFirstObjectByType<CinemachineCamera>();
        var arena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var playerObject = Object.Instantiate(sourcePlayer.gameObject);
        var cameraObject = Object.Instantiate(sourceCamera.gameObject);
        var outputObject = Object.Instantiate(Camera.main.transform.root.gameObject);
        SceneManager.MoveGameObjectToScene(playerObject, arena);
        SceneManager.MoveGameObjectToScene(cameraObject, arena);
        SceneManager.MoveGameObjectToScene(outputObject, arena);
        if (includePole)
        {
            var pole = Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None)
                .First(c => c.gameObject.layer == LayerMask.NameToLayer("poleLayer"));
            var copy = Object.Instantiate(pole.gameObject, pole.transform.position, pole.transform.rotation);
            copy.name = "Arena pole collider";
            copy.transform.localScale = pole.transform.lossyScale;
            SceneManager.MoveGameObjectToScene(copy, arena);
            copy.transform.position = Vector3.right * 40f;
        }
        EditorSceneManager.CloseScene(sourceScene, true);
        SceneManager.SetActiveScene(arena);

        foreach (var behaviour in playerObject.GetComponentsInChildren<MonoBehaviour>(true))
        {
            behaviour.enabled = behaviour is PlayerInputHandler || behaviour is PlayerMovementIntent ||
                behaviour is CharacterMovement || behaviour is CharacterState || behaviour is StaminaSystem ||
                behaviour is BuffNerfController || behaviour is SlideExecutor ||
                behaviour is VaultExecutor || behaviour is ClimbExecutor;
        }
        // The source has a separate full-height capsule; isolate locomotion's controller.
        foreach (var collider in playerObject.GetComponentsInChildren<Collider>())
            if (!(collider is CharacterController)) collider.enabled = false;

        var output = outputObject.GetComponentInChildren<Camera>();
        var player = playerObject.GetComponent<PlayerMovementIntent>();
        var camera = cameraObject.GetComponent<CinemachineCamera>();
        camera.Follow = player.transform;
        var intentData = new SerializedObject(player);
        intentData.FindProperty("cameraTransform").objectReferenceValue = output.transform;
        intentData.ApplyModifiedPropertiesWithoutUndo();
        var slideData = new SerializedObject(playerObject.GetComponent<SlideExecutor>());
        slideData.FindProperty("standingClearanceLayer").intValue = 1 << 6;
        slideData.ApplyModifiedPropertiesWithoutUndo();
        var controller = playerObject.GetComponent<CharacterController>();
        player.transform.SetPositionAndRotation(
            new Vector3(0f, controller.height * 0.5f - controller.center.y + 0.05f, 0f), Quaternion.identity);
        CreateBox("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(300f, 1f, 300f));
    }

    private void PrepareInput()
    {
        if (Application.isBatchMode)
        {
            // Batch mode has no focused Game view to receive the simulated devices.
            // A fixed game delta also keeps controller steps above its minimum move distance.
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            originalInputSettings = InputSystem.settings;
            var settings = Object.Instantiate(originalInputSettings);
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = settings;
        }
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
    }

    [UnityTest]
    public IEnumerator MovementFeedbackAndLookRemainIndependentInPlayMode()
    {
        PrepareArena();
        yield return new EnterPlayMode();
        PrepareInput();
        var player = Object.FindFirstObjectByType<PlayerMovementIntent>();
        var camera = Object.FindFirstObjectByType<CinemachineCamera>();
        var movement = player.GetComponent<CharacterMovement>();
        var stamina = player.GetComponent<StaminaSystem>();
        var slide = player.GetComponent<SlideExecutor>();
        var feedback = camera.GetComponent<MovementCameraFeedback>();
        var orbit = camera.GetComponent<CinemachineOrbitalFollow>();
        var deoccluder = camera.GetComponent<CinemachineDeoccluder>();
        float baseline = camera.Lens.FieldOfView;
        yield return Settle(0.5f);
        Assert.That(feedback.CurrentFovOffset, Is.EqualTo(0f).Within(0.01f), "idle FOV");

        Keys(Key.W, Key.LeftShift);
        yield return Settle(1.1f);
        Assert.That(player.Sprinting, Is.True, "real sprint input reached intent");
        Assert.That(feedback.CurrentFovOffset, Is.InRange(3.5f, 5.05f), "moving sprint FOV");
        Assert.That(camera.State.Lens.FieldOfView, Is.GreaterThan(baseline + 3.5f));
        Assert.That(camera.Lens.FieldOfView, Is.EqualTo(baseline), "authored lens never accumulates offsets");

        Keys(Key.LeftShift);
        yield return Settle(0.6f);
        Assert.That(feedback.CurrentFovOffset, Is.LessThan(0.05f), "stationary sprint");

        stamina.ConsumeContinuous(stamina.MaxStamina * 100f);
        Keys(Key.W, Key.LeftShift);
        yield return Settle(0.6f);
        Assert.That(stamina.IsExhausted, Is.True);
        Assert.That(feedback.CurrentFovOffset, Is.LessThan(0.05f), "exhausted sprint");
        Keys();
        stamina.Restore(stamina.MaxStamina);
        yield return Settle(0.3f);

        var wall = CreateBox("Sprint blocker", player.transform.position + Vector3.forward * 2f,
            new Vector3(30f, 20f, 1f));
        Keys(Key.W, Key.LeftShift);
        yield return Settle(1f);
        Assert.That(feedback.CurrentFovOffset, Is.LessThan(0.1f), "blocked sprint uses actual displacement");
        Keys();
        Object.Destroy(wall);
        yield return Settle(0.4f);

        float yaw = camera.State.GetFinalOrientation().eulerAngles.y;
        float axis = orbit.HorizontalAxis.Value;
        // Freeze actor movement for this independent rotation check.
        movement.enabled = false;
        player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        yield return Settle(0.5f);
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw, camera.State.GetFinalOrientation().eulerAngles.y)), Is.LessThan(0.1f));
        Assert.That(orbit.HorizontalAxis.Value, Is.EqualTo(axis).Within(0.001f));

        InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(100f, 25f) });
        yield return Settle(0.3f);
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(axis, orbit.HorizontalAxis.Value)), Is.GreaterThan(0.01f), "Cinemachine still receives look input");
        orbit.HorizontalAxis.Value = 0f;
        orbit.VerticalAxis.Value = 17.5f;
        movement.enabled = true;
        yield return Settle(0.5f);

        // Zero travel direction keeps the slide below the test ceiling.
        var ceiling = CreateBox("Low ceiling", player.transform.position + Vector3.up * 2.6f,
            new Vector3(12f, 0.5f, 12f));
        ceiling.layer = 6;
        Assert.That(slide.TryExecute(Vector3.zero), Is.True);
        yield return Settle(0.9f);
        Assert.That(slide.IsExecuting, Is.True, "blocked standing extends slide past its nominal duration");
        Assert.That(feedback.CurrentHeightOffset, Is.EqualTo(-0.45f).Within(0.02f));
        Object.Destroy(ceiling);
        yield return Settle(0.6f);
        Assert.That(slide.IsExecuting, Is.False);
        Assert.That(feedback.CurrentHeightOffset, Is.EqualTo(0f).Within(0.02f));

        stamina.Restore(stamina.MaxStamina);
        Keys(Key.W, Key.LeftShift);
        Assert.That(slide.TryExecute(Vector3.forward), Is.True);
        yield return Settle(0.25f);
        Assert.That(feedback.CurrentFovOffset, Is.InRange(4f, 7.05f), "moving slide FOV is bounded");
        float offsetBeforePause = feedback.CurrentHeightOffset;
        Time.timeScale = 0f;
        for (int i = 0; i < 5; i++) yield return null;
        Assert.That(feedback.CurrentHeightOffset, Is.EqualTo(offsetBeforePause).Within(0.001f), "pause freezes feedback");
        Time.timeScale = 1f;
        Keys();
        yield return Settle(0.8f);

        stamina.Restore(stamina.MaxStamina);
        Assert.That(movement.TryJump(), Is.True);
        float mostNegativeOffset = 0f;
        float jumpEnd = Time.time + 2.5f;
        while (Time.time < jumpEnd)
        {
            mostNegativeOffset = Mathf.Min(mostNegativeOffset, feedback.CurrentHeightOffset);
            yield return null;
        }
        Assert.That(mostNegativeOffset, Is.LessThan(-0.005f), "landing produces a small dip");
        Assert.That(feedback.CurrentHeightOffset, Is.EqualTo(0f).Within(0.02f), "landing recovers");

        // Collision runs against the corrected pose; entity colliders are excluded.
        var obstruction = CreateBox("Camera obstruction", camera.State.ReferenceLookAt + Vector3.back * 3f,
            new Vector3(20f, 15f, 1f));
        yield return Settle(0.4f);
        Assert.That(deoccluder.CameraWasDisplaced(camera), Is.True);
        Assert.That(obstruction.GetComponent<Collider>().bounds.Contains(camera.State.GetFinalPosition()), Is.False);
        Object.Destroy(obstruction);
        yield return Settle(0.4f);

        Keys(Key.W, Key.LeftShift);
        yield return Settle(0.8f);
        feedback.enabled = false;
        yield return Settle(0.1f);
        Assert.That(camera.State.Lens.FieldOfView, Is.EqualTo(baseline).Within(0.01f), "disable restores authored lens");
        feedback.enabled = true;
        Keys();
        camera.OnTargetObjectWarped(player.transform, Vector3.zero);
        yield return Settle(0.3f);
        Assert.That(feedback.CurrentFovOffset, Is.LessThan(0.05f));
        Assert.That(feedback.CurrentHeightOffset, Is.EqualTo(0f).Within(0.02f));

        yield return VerifyTraversalShots(player, camera, feedback);
    }

    private IEnumerator VerifyTraversalShots(PlayerMovementIntent player, CinemachineCamera camera,
        MovementCameraFeedback feedback)
    {
        var vault = player.GetComponent<VaultExecutor>();
        var climb = player.GetComponent<ClimbExecutor>();
        var orbit = camera.GetComponent<CinemachineOrbitalFollow>();
        var controller = player.GetComponent<CharacterController>();
        player.GetComponent<StaminaSystem>().Restore(100f);
        Vector3 start = player.transform.position;
        Vector3 entry = camera.State.GetFinalPosition();
        Assert.That(vault.TryExecute(new VaultCandidate
        {
            IsValid = true, RequiredArcHeight = 1f, LandingPosition = start + Vector3.right * 3f,
            LandingSurfacePosition = start + Vector3.right * 3f +
                Vector3.up * (controller.center.y - controller.height * 0.5f)
        }), Is.True);
        yield return Settle(0.22f);
        Assert.That(feedback.CurrentFovOffset, Is.InRange(7.5f, 8.01f));
        Vector3 held = camera.State.GetFinalPosition();
        Assert.That(Vector3.Distance(held, entry + Vector3.up * 0.30f), Is.LessThan(0.08f));
        orbit.HorizontalAxis.Value += 30f;
        yield return Settle(0.12f);
        Assert.That(vault.IsExecuting, Is.True);
        Assert.That(Vector3.Distance(held, camera.State.GetFinalPosition()), Is.LessThan(0.03f),
            "world position holds while actor and orbit both move");

        // Put an obstacle on the held line of sight, away from the new orbit.
        Vector3 focus = camera.State.ReferenceLookAt;
        var blocker = CreateBox("Vault camera blocker", Vector3.Lerp(focus, held, 0.65f), Vector3.one * 1.7f);
        yield return Settle(0.08f);
        Assert.That(Vector3.Distance(camera.State.GetFinalPosition(), focus),
            Is.LessThan(Vector3.Distance(held, focus) - 0.5f), "collision takes priority over the hold");
        Object.Destroy(blocker);
        while (vault.IsExecuting) yield return null;
        yield return Settle(0.16f);
        Assert.That(camera.State.PositionCorrection.magnitude, Is.GreaterThan(0.2f),
            "camera has not snapped to its orbit at vault exit");

        // Chain from a partially recovered vault directly into a climb.
        entry = camera.State.GetFinalPosition();
        start = player.transform.position;
        float bottom = controller.center.y - controller.height * 0.5f;
        Assert.That(climb.TryExecute(new ClimbCandidate
        {
            IsValid = true, TopPosition = start + Vector3.up * (bottom + 2.1f) + Vector3.forward * 1.5f
        }), Is.True);
        yield return Settle(0.22f);
        Assert.That(climb.IsExecuting, Is.True);
        Assert.That(feedback.CurrentFovOffset, Is.InRange(6.7f, 7.1f));
        Assert.That(Vector3.Distance(camera.State.GetFinalPosition(), entry + Vector3.up * 0.45f),
            Is.LessThan(0.18f), "climb uses the current shot as its new anchor");
        while (climb.IsExecuting) yield return null;
        Keys(Key.W);
        yield return Settle(0.15f);
        Assert.That(camera.State.PositionCorrection.magnitude, Is.GreaterThan(0.15f));
        yield return Settle(0.85f);
        Keys();
        Assert.That(camera.State.PositionCorrection.magnitude, Is.LessThan(0.03f),
            "return finishes at the moving live orbit");
        Assert.That(feedback.CurrentFovOffset, Is.LessThan(0.05f));

        // At half strength the camera shares the player's travel. Starting recovery
        // must not apply that half weight a second time and jump toward the orbit.
        var tuning = new SerializedObject(feedback);
        tuning.FindProperty("traversalPositionIntensity").floatValue = 0.5f;
        tuning.ApplyModifiedPropertiesWithoutUndo();
        yield return Settle(0.6f);
        start = player.transform.position;
        entry = camera.State.GetFinalPosition();
        Vector3 liveEntry = camera.State.RawPosition;
        player.GetComponent<StaminaSystem>().Restore(100f);
        Assert.That(vault.TryExecute(new VaultCandidate
        {
            IsValid = true, RequiredArcHeight = 0.8f, LandingPosition = start + Vector3.right * 4f
        }), Is.True);
        yield return Settle(0.32f);
        Vector3 expected = entry + (camera.State.RawPosition - liveEntry) * 0.5f + Vector3.up * 0.15f;
        Assert.That(Vector3.Distance(expected, camera.State.GetFinalPosition()), Is.LessThan(0.12f));
        Vector3 lastHeld = camera.State.GetFinalPosition();
        while (vault.IsExecuting)
        {
            lastHeld = camera.State.GetFinalPosition();
            yield return null;
        }
        Assert.That(Vector3.Distance(lastHeld, camera.State.GetFinalPosition()), Is.LessThan(0.35f));
        yield return Settle(0.8f);
        Assert.That(camera.State.PositionCorrection.magnitude, Is.LessThan(0.03f));
    }

    private static readonly string[] PoleScenes = { "Gameplay", "AI-NAV-TEST2" };

    [UnityTest]
    public IEnumerator ReboundsAndPoleSpinUseActionDirectionAndPivotInPlayMode(
        [ValueSource(nameof(PoleScenes))] string sceneName)
    {
        PrepareArena(sceneName, true);
        yield return new EnterPlayMode();
        PrepareInput();
        var actor = Object.FindFirstObjectByType<PlayerMovementIntent>();
        var camera = Object.FindFirstObjectByType<CinemachineCamera>();
        var feedback = camera.GetComponent<MovementCameraFeedback>();
        var orbit = camera.GetComponent<TraversalOrbitalFollow>();
        var rebound = actor.GetComponent<WallReboundExecutor>();
        var ticTac = actor.GetComponent<TicTacExecutor>();
        var pole = actor.GetComponent<PoleSpinExecutor>();
        rebound.enabled = ticTac.enabled = pole.enabled = true;
        yield return Settle(0.6f);

        Vector3 entry = camera.State.GetFinalPosition();
        float entryHeight = entry.y - actor.transform.position.y;
        float originalYaw = orbit.HorizontalAxis.Value;
        Vector3 direction = Vector3.right;
        Assert.That(rebound.TryExecute(new WallReboundCandidate
            { IsValid = true, ReboundDirection = direction, ImpactSpeed = 5f }), Is.True);
        yield return Settle(0.10f);
        Assert.That(Vector3.Distance(entry, camera.State.GetFinalPosition()), Is.LessThan(0.04f), "launch delay");
        yield return Settle(0.62f);
        Assert.That(rebound.IsExecuting, Is.False, "the short rebound has already finished");
        Vector3 arm = camera.State.GetFinalPosition() - actor.transform.position;
        Assert.That(Vector3.Dot(Vector3.ProjectOnPlane(arm, Vector3.up).normalized, direction), Is.LessThan(-0.98f));
        Assert.That(arm.y - entryHeight, Is.EqualTo(4f).Within(0.2f));
        Assert.That(feedback.CurrentFovOffset, Is.EqualTo(5f).Within(0.1f));
        Assert.That(orbit.HorizontalAxis.Value, Is.EqualTo(originalYaw).Within(0.01f));

        // Interrupt the return with a differently directed TicTac.
        yield return Settle(0.24f);
        entry = camera.State.GetFinalPosition();
        direction = Vector3.forward;
        Assert.That(ticTac.TryExecute(new TicTacCandidate
            { IsValid = true, TicTacDirection = direction, LaunchSpeed = 4f }), Is.True);
        yield return Settle(0.08f);
        Assert.That(Vector3.Distance(entry, camera.State.GetFinalPosition()), Is.LessThan(0.2f));
        yield return Settle(0.65f);
        arm = camera.State.GetFinalPosition() - actor.transform.position;
        Assert.That(Vector3.Dot(Vector3.ProjectOnPlane(arm, Vector3.up).normalized, direction), Is.LessThan(-0.98f));
        yield return Settle(1.8f);
        Assert.That(camera.State.PositionCorrection.magnitude, Is.LessThan(0.04f));
        Assert.That(feedback.CurrentFovOffset, Is.LessThan(0.05f));

        // Use the shipped pole's collider, the real detector and the real spin executor.
        var collider = GameObject.Find("Arena pole collider").GetComponent<CapsuleCollider>();
        var detector = actor.GetComponent<ParkourDetector>();
        collider.transform.position += actor.transform.position + Vector3.right * (collider.bounds.extents.x + 0.95f)
            + Vector3.up * 1.4f - collider.bounds.center;
        Physics.SyncTransforms();
        detector.enabled = true;
        yield return Settle(0.1f);
        actor.GetComponent<StaminaSystem>().Restore(100f);
        var candidate = actor.GetComponent<PoleSpinEvaluator>().Evaluate(detector.CurrentPole);
        Assert.That(candidate.IsValid, Is.True, "authored pole detected");
        entry = camera.State.GetFinalPosition();
        Assert.That(pole.TryExecute(candidate), Is.True);
        Vector3 pivot = pole.CurrentPolePosition;
        yield return Settle(0.6f);
        Assert.That(feedback.CurrentFovOffset, Is.EqualTo(5f).Within(0.1f));
        Assert.That(camera.State.GetFinalPosition().y - entry.y, Is.EqualTo(5f).Within(0.15f), "pole lift");
        Vector3 focus = pivot + actor.transform.rotation * camera.GetComponent<CinemachineRotationComposer>().TargetOffset;
        Assert.That(Vector3.Distance(camera.State.ReferenceLookAt, focus), Is.LessThan(0.1f), "pole framing");
        float actorTravel = 0f, cameraTravel = 0f;
        Vector3 actorArm = actor.transform.position - pivot, cameraArm = camera.State.GetFinalPosition() - pivot;
        float end = Time.time + 0.6f;
        while (Time.time < end)
        {
            yield return null;
            Vector3 nextActor = actor.transform.position - pivot;
            Vector3 nextCamera = camera.State.GetFinalPosition() - pivot;
            actorTravel += PlanarTurn(actorArm, nextActor);
            cameraTravel += PlanarTurn(cameraArm, nextCamera);
            actorArm = nextActor;
            cameraArm = nextCamera;
        }
        Assert.That(Mathf.Abs(actorTravel), Is.GreaterThan(75f));
        Assert.That(cameraTravel, Is.EqualTo(actorTravel * 0.25f).Within(2f), "slower signed orbit");
        var collision = camera.GetComponent<CinemachineDeoccluder>();
        var sightline = (camera.State.GetFinalPosition() - focus).normalized;
        var blocker = CreateBox("Wall beside the pole", focus + sightline * (collider.bounds.extents.x + 0.85f),
            new Vector3(1.5f, 2.5f, 0.25f));
        blocker.transform.rotation = Quaternion.LookRotation(sightline);
        yield return Settle(0.05f);
        Assert.That(collision.CameraWasDisplaced(camera), Is.True, "wall against the active pole still occludes");
        Vector3 corrected = camera.State.GetFinalPosition();
        Assert.That(Vector3.Distance(collider.ClosestPoint(corrected), corrected), Is.GreaterThan(0.01f),
            "the wall must not push the camera inside the pole");
        Assert.That(Vector3.Distance(blocker.GetComponent<Collider>().ClosestPoint(corrected), corrected),
            Is.GreaterThan(0.01f), "camera clears the angled wall");
        Object.Destroy(blocker);
        yield return Settle(0.4f);
        var secondPole = CreateBox("Second pole", Vector3.Lerp(focus, camera.State.GetFinalPosition(), 0.65f),
            new Vector3(1.2f, 5f, 1.2f));
        secondPole.layer = collider.gameObject.layer;
        yield return Settle(0.05f);
        Assert.That(collision.CameraWasDisplaced(camera), Is.True, "a second pole can still obstruct the shot");
        Object.Destroy(secondPole);
        pole.FinishSpin();
        InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(80f, 0f) });
        yield return Settle(1.8f);
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(originalYaw, orbit.HorizontalAxis.Value)), Is.GreaterThan(0.01f));
        Assert.That(feedback.CurrentFovOffset, Is.LessThan(0.05f));
        Assert.That(camera.State.PositionCorrection.magnitude, Is.LessThan(0.05f), "release returns to live orbit");
    }

    private static float PlanarTurn(Vector3 from, Vector3 to) =>
        Vector3.SignedAngle(Vector3.ProjectOnPlane(from, Vector3.up), Vector3.ProjectOnPlane(to, Vector3.up), Vector3.up);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (originalInputSettings != null)
        {
            Time.captureDeltaTime = previousCaptureDeltaTime;
            var temporarySettings = InputSystem.settings;
            InputSystem.settings = originalInputSettings;
            Object.DestroyImmediate(temporarySettings);
            originalInputSettings = null;
        }
        if (Application.isPlaying) yield return new ExitPlayMode();
        string savedSetup = SessionState.GetString(SceneSetupKey, "");
        SessionState.EraseString(SceneSetupKey);
        if (!string.IsNullOrEmpty(savedSetup))
        {
            var scenes = JsonUtility.FromJson<SavedScenes>(savedSetup).scenes;
            if (scenes.Length == 0 || scenes.Any(x => string.IsNullOrEmpty(x.path)))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            else
                EditorSceneManager.RestoreSceneManagerSetup(scenes);
        }
    }

    private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private static IEnumerator Settle(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end) yield return null;
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        return box;
    }
}
