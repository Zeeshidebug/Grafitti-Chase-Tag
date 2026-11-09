using Unity.Cinemachine;
using UnityEngine;

// Keep calculating the live orbit even when the traversal shot is holding still.
// Body extensions (in particular Deoccluder) always receive the adjusted pose.
[AddComponentMenu("Cinemachine/Procedural/Position Control/Traversal Orbital Follow")]
public sealed class TraversalOrbitalFollow : CinemachineOrbitalFollow
{
    private MovementCameraFeedback feedback;

    protected override void OnEnable()
    {
        base.OnEnable();
        TryGetComponent(out feedback);
    }

    public override void MutateCameraState(ref CameraState state, float deltaTime)
    {
        base.MutateCameraState(ref state, deltaTime);
        if (Application.isPlaying && feedback != null && feedback.isActiveAndEnabled)
            feedback.ApplyTraversalBody(ref state);
    }
}
