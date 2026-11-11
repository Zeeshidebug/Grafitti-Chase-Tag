using Unity.Cinemachine;
using UnityEngine;

[AddComponentMenu("Cinemachine/Procedural/Extensions/Traversal Deoccluder")]
public sealed class TraversalDeoccluder : CinemachineDeoccluder
{
    private MovementCameraFeedback feedback;
    private readonly Collider[] polesOnSightline = new Collider[2];

    protected override void OnEnable()
    {
        base.OnEnable();
        TryGetComponent(out feedback);
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        Collider pole = feedback != null && feedback.isActiveAndEnabled ? feedback.FramedPole : null;
        if (pole == null || !state.HasLookAt() || AvoidObstacles.UseFollowTarget.Enabled ||
            (stage != CinemachineCore.Stage.Body && stage != CinemachineCore.Stage.Finalize))
        {
            base.PostPipelineStageCallback(vcam, stage, ref state, deltaTime);
            return;
        }
        if (stage == CinemachineCore.Stage.Finalize && !ShotQualityEvaluation.Enabled)
            return;

        // Only the pole we're spinning on can be transparent. If the camera
        // touches one or a second pole is on this sightline, use normal collision.
        int poleLayer = 1 << pole.gameObject.layer;
        Vector3 position = state.GetCorrectedPosition();
        float radius = AvoidObstacles.CameraRadius;
        LayerMask authoredLayers = TransparentLayers;
        bool checkPole = (CollideAgainst.value & ~authoredLayers.value & poleLayer) != 0 &&
            pole.bounds.SqrDistance(position) > radius * radius;
        if (checkPole && Physics.OverlapCapsuleNonAlloc(state.ReferenceLookAt, position, Mathf.Max(0.001f, radius),
                polesOnSightline, poleLayer, QueryTriggerInteraction.Ignore) == 1 && polesOnSightline[0] == pole)
            TransparentLayers = authoredLayers.value | poleLayer;
        try
        {
            base.PostPipelineStageCallback(vcam, stage, ref state, deltaTime);
        }
        finally
        {
            TransparentLayers = authoredLayers;
        }
    }
}
