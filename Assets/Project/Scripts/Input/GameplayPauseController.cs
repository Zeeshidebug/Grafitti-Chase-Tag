using Unity.Cinemachine;
using UnityEngine;

// Only the gameplay player owns pause and the desktop cursor.
[RequireComponent(typeof(PlayerInputHandler))]
[DefaultExecutionOrder(-210)]
public class GameplayPauseController : MonoBehaviour
{
    [SerializeField] private CinemachineInputAxisController cameraInput;
    [SerializeField] private RoundController roundController;

    private PlayerInputHandler input;
    private bool hasFocus;
    private bool ownsPause;
    private bool suspendedCameraInput;
    private float previousTimeScale;

    public static bool IsPaused => Time.timeScale == 0f;
    public static float UnpausedUnscaledDeltaTime => IsPaused ? 0f : Time.unscaledDeltaTime;

    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        hasFocus = Application.isFocused;
    }

    private void Update()
    {
        if (hasFocus && input.PausePressed)
        {
            if (ownsPause)
                Resume();
            else if (!IsPaused)
            {
                previousTimeScale = Time.timeScale;
                ownsPause = true;
                Time.timeScale = 0f;
            }
        }

        SetCapture(hasFocus && !IsPaused && !input.FreeCursorHeld);
    }

    private void OnApplicationFocus(bool focused)
    {
        hasFocus = focused;
        if (!focused && isActiveAndEnabled)
            SetCapture(false);
    }

    private void SetCapture(bool capture)
    {
        SetCursor(capture);
        if (cameraInput == null)
            return;

        bool allowLook = capture && (roundController == null || roundController.CurrentState == RoundState.Playing);
        if (!allowLook && cameraInput.enabled)
        {
            cameraInput.enabled = false;
            suspendedCameraInput = true;
        }
        else if (allowLook && suspendedCameraInput)
            RestoreCameraInput();
    }

    private static void SetCursor(bool capture)
    {
        var mode = capture ? CursorLockMode.Locked : CursorLockMode.None;
        if (Cursor.lockState != mode)
            Cursor.lockState = mode;
        bool visible = !capture;
        if (Cursor.visible != visible)
            Cursor.visible = visible;
    }

    private void RestoreCameraInput()
    {
        if (suspendedCameraInput && cameraInput != null)
            cameraInput.enabled = true;
        suspendedCameraInput = false;
    }

    private void Resume()
    {
        Time.timeScale = previousTimeScale;
        ownsPause = false;
    }

    private void OnDisable()
    {
        if (ownsPause)
            Resume();
        RestoreCameraInput();
        SetCursor(false);
    }

    private void OnGUI()
    {
        if (ownsPause)
            GUI.Box(new Rect((Screen.width - 220f) / 2f, (Screen.height - 52f) / 2f, 220f, 52f),
                "PAUSED\nEsc / Start to resume");
    }
}
