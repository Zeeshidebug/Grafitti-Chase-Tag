using UnityEngine;

// World-up geometry for the launch and pole shots. The native orbit continues
// to run underneath, so the return destination includes the player's mouse input.
internal sealed class ActionCameraOrbit
{
    public Vector3 Pivot { get; private set; }

    private Vector3 origin, startCentre, startArm, launchHeading, poleHeading, previousRadial;
    private float targetRadius, targetHeight, turn, returnTurn;
    private bool waitingForTurn;

    public void BeginLaunch(Vector3 cameraPosition, Vector3 liveArm, Vector3 direction, Vector3 actorForward)
    {
        origin = cameraPosition;
        launchHeading = Radial(direction, actorForward);
        targetRadius = Mathf.Max(0.1f, Planar(liveArm).magnitude);
        targetHeight = liveArm.y;
        waitingForTurn = true;
    }

    public Vector3 LaunchPosition(Vector3 actor, float delay, float elapsed, float duration, float lift)
    {
        if (elapsed <= delay)
            return origin;

        if (waitingForTurn)
        {
            // Measure at the end of the delay: the actor has already bounced away.
            startCentre = actor;
            startArm = origin - actor;
            turn = Vector3.SignedAngle(Radial(startArm, -launchHeading), -launchHeading, Vector3.up);
            waitingForTurn = false;
        }
        float t = Mathf.SmoothStep(0f, 1f, (elapsed - delay) / Mathf.Max(0.01f, duration));
        return Arc(Vector3.Lerp(startCentre, actor, t), startArm, -launchHeading, turn, targetRadius, targetHeight + lift, t);
    }

    public void BeginPole(Vector3 cameraPosition, Vector3 liveArm, Vector3 pivot, Vector3 actor)
    {
        Pivot = pivot;
        startArm = cameraPosition - pivot;
        previousRadial = Planar(actor - pivot);
        poleHeading = Radial(startArm, liveArm);
        targetRadius = Mathf.Max(Planar(startArm).magnitude, Planar(liveArm).magnitude);
        turn = 0f;
    }

    public void AdvancePole(Vector3 actor, float fraction, float maxStep)
    {
        Vector3 radial = Planar(actor - Pivot);
        if (radial.sqrMagnitude > 0.0001f && previousRadial.sqrMagnitude > 0.0001f)
        {
            float travelled = Vector3.SignedAngle(previousRadial, radial, Vector3.up);
            turn = Mathf.Repeat(turn + Mathf.Clamp(travelled * fraction, -maxStep, maxStep), 360f);
        }
        previousRadial = radial;
    }

    public Vector3 PolePosition(float lift, float entryBlend)
    {
        float radius = Mathf.Lerp(Planar(startArm).magnitude, targetRadius, entryBlend);
        return Pivot + Quaternion.AngleAxis(turn, Vector3.up) * poleHeading * radius
            + Vector3.up * (startArm.y + lift * entryBlend);
    }

    public void BeginReturn(Vector3 cameraPosition, Vector3 actor)
    {
        startCentre = actor;
        startArm = cameraPosition - actor;
        returnTurn = 0f;
    }

    public Vector3 ReturnPosition(Vector3 actor, Vector3 liveOrbit, float t)
    {
        Vector3 liveArm = liveOrbit - actor;
        Vector3 direction = Radial(startArm, liveArm);
        float angle = Vector3.SignedAngle(direction, Radial(liveArm, direction), Vector3.up);
        // Unwrap the moving destination so crossing +/-180 doesn't flip the arc.
        returnTurn += Mathf.DeltaAngle(returnTurn, angle);
        return Arc(Vector3.Lerp(startCentre, actor, t), startArm, direction, returnTurn,
            Planar(liveArm).magnitude, liveArm.y, t);
    }

    private static Vector3 Arc(Vector3 centre, Vector3 from, Vector3 fallback, float degrees,
        float radius, float height, float t)
    {
        Vector3 direction = Quaternion.AngleAxis(degrees * t, Vector3.up) * Radial(from, fallback);
        return centre + direction * Mathf.Lerp(Planar(from).magnitude, radius, t)
            + Vector3.up * Mathf.Lerp(from.y, height, t);
    }

    private static Vector3 Radial(Vector3 arm, Vector3 fallback)
    {
        Vector3 flat = Planar(arm);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Planar(fallback);
        return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.back;
    }

    private static Vector3 Planar(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
