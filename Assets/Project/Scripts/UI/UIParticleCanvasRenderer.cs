using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the native ParticleSystem mesh inside a uGUI Canvas, including overlay
/// sorting, CanvasGroup alpha and stencil masks. Motion is authored on ParticleSystem.
/// </summary>
[RequireComponent(typeof(ParticleSystem), typeof(CanvasRenderer))]
public sealed class UIParticleCanvasRenderer : MaskableGraphic
{
    private ParticleSystem source;
    private ParticleSystemRenderer sourceRenderer;
    private Mesh particleMesh;
    private Camera bakeCamera;
    private bool hadParticles, simulating;

    protected override void OnEnable()
    {
        base.OnEnable();
        source = GetComponent<ParticleSystem>();
        sourceRenderer = GetComponent<ParticleSystemRenderer>();
        // The Canvas renders this mesh; don't draw a second copy in the world.
        sourceRenderer.enabled = false;
        raycastTarget = false;
        if (particleMesh == null)
        {
            particleMesh = new Mesh { name = "UI ParticleSystem mesh", hideFlags = HideFlags.HideAndDontSave };
            particleMesh.MarkDynamic();
        }
    }

    private void LateUpdate()
    {
        // Overlay emitters are outside the camera render loop. Advance Unity's
        // native simulation explicitly, then submit its baked mesh to uGUI.
        if (simulating && source != null)
        {
            float dt = GameplayPauseController.UnpausedUnscaledDeltaTime;
            if (dt > 0f) source.Simulate(dt, false, false, false);
            if (!source.IsAlive(false)) simulating = false;
        }
        bool hasParticles = source != null && source.particleCount > 0;
        if (hasParticles || hadParticles) SetVerticesDirty();
        hadParticles = hasParticles;
    }

    public static void PlayBurst(ParticleSystem particles)
    {
        if (particles == null) return;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var output = particles.GetComponent<UIParticleCanvasRenderer>();
        if (output == null)
        {
            particles.Play(true);
            return;
        }
        // Simulate leaves the system paused, preventing a second automatic tick.
        particles.Simulate(0f, false, true, false);
        output.simulating = true;
        output.SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        simulating = hadParticles = false;
        if (source != null) source.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        base.OnDisable();
    }

    protected override void UpdateGeometry()
    {
        if (particleMesh == null || sourceRenderer == null) return;
        particleMesh.Clear();
        if (source.particleCount > 0)
        {
            if (bakeCamera == null)
                bakeCamera = canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (bakeCamera != null)
            {
                // Local simulation and local mesh coordinates avoid double-applying
                // the Canvas scale and the role bar's squash/stretch transform.
                sourceRenderer.BakeMesh(particleMesh, bakeCamera, (ParticleSystemBakeMeshOptions)0);
            }
        }
        canvasRenderer.SetMesh(particleMesh);
    }

    protected override void OnDestroy()
    {
        if (particleMesh != null)
        {
            if (Application.isPlaying) Destroy(particleMesh);
            else DestroyImmediate(particleMesh);
        }
        base.OnDestroy();
    }
}
