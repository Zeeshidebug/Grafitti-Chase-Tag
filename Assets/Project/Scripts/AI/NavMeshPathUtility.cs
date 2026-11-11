using UnityEngine;
using UnityEngine.AI;

/// <summary>Reads complete paths into reusable buffers without truncating long routes.</summary>
public static class NavMeshPathUtility
{
    public static int ReadCorners(NavMeshPath path, ref Vector3[] buffer)
    {
        if (path == null)
            return 0;

        if (buffer == null || buffer.Length < 2)
            buffer = new Vector3[16];

        int count = path.GetCornersNonAlloc(buffer);
        if (count > 0 && count < buffer.Length)
            return count;

        // A full buffer may contain only part of the route. Use the complete
        // array on overflow (including APIs returning zero), then retain space
        // for subsequent paths. Empty paths also return an explicit zero count.
        Vector3[] corners = path.corners;
        if (corners.Length >= buffer.Length)
            buffer = new Vector3[Mathf.Max(buffer.Length * 2, corners.Length + 1)];

        System.Array.Copy(corners, buffer, corners.Length);
        return corners.Length;
    }

    public static float CalculateLength(NavMeshPath path, ref Vector3[] buffer)
    {
        int count = ReadCorners(path, ref buffer);
        float length = 0f;
        for (int i = 1; i < count; i++)
            length += Vector3.Distance(buffer[i - 1], buffer[i]);

        return length;
    }
}
