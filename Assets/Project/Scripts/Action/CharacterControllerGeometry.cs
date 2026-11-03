using UnityEngine;

/// <summary>Shared world-space geometry helpers for CharacterController.</summary>
public static class CharacterControllerGeometry
{
    public static void GetCapsulePoints(
        CharacterController controller,
        Vector3 center,
        out Vector3 point1,
        out Vector3 point2,
        out float radius)
    {
        point1 = center;
        point2 = center;
        radius = 0f;

        if (controller == null)
            return;

        radius = controller.radius;
        float height = Mathf.Max(controller.height, radius * 2f);
        float cylinderHeight = height - radius * 2f;

        point1 = center + Vector3.up * (cylinderHeight * 0.5f);
        point2 = center - Vector3.up * (cylinderHeight * 0.5f);
    }

    public static Vector3 GetWorldBottomOffset(CharacterController controller)
    {
        if (controller == null)
            return Vector3.zero;

        Vector3 localBottom = controller.center
            - Vector3.up * (controller.height * 0.5f);
        return controller.transform.TransformVector(localBottom);
    }
}
