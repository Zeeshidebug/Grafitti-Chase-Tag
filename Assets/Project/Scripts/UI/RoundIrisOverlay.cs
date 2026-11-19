using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class RoundIrisOverlay : MaskableGraphic
{
    [SerializeField, Range(0f, 1f)] private float reveal;
    private readonly float[] angles = new float[164];
    private Vector2 previousSize;
    public float Reveal => reveal;

    public void SetReveal(float value)
    {
        reveal = Mathf.Clamp01(value);
        raycastTarget = reveal < 1f;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (reveal >= 1f) return;
        Rect r = rectTransform.rect;
        Vector2 half = r.size * 0.5f;
        if (half.x <= 0f || half.y <= 0f) return;
        if (previousSize != r.size)
        {
            for (int i = 0; i < 160; i++) angles[i] = i * Mathf.PI * 2f / 160;
            // Include exact rectangle corners so no uncovered corner slivers appear.
            float corner = Mathf.Atan2(half.y, half.x);
            angles[160] = corner;
            angles[161] = Mathf.PI - corner;
            angles[162] = Mathf.PI + corner;
            angles[163] = 2f * Mathf.PI - corner;
            Array.Sort(angles);
            previousSize = r.size;
        }
        float radius = reveal * (half.magnitude + 2f);
        for (int i = 0; i < angles.Length; i++)
        {
            Vector2 a = new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i]));
            Vector2 b = new Vector2(Mathf.Cos(angles[(i + 1) % angles.Length]), Mathf.Sin(angles[(i + 1) % angles.Length]));
            float da = EdgeDistance(a, half), db = EdgeDistance(b, half);
            int n = mesh.currentVertCount;
            mesh.AddVert(r.center + a * Mathf.Min(radius, da), color, Vector2.zero);
            mesh.AddVert(r.center + a * da, color, Vector2.zero);
            mesh.AddVert(r.center + b * db, color, Vector2.zero);
            mesh.AddVert(r.center + b * Mathf.Min(radius, db), color, Vector2.zero);
            mesh.AddTriangle(n, n + 1, n + 2);
            mesh.AddTriangle(n, n + 2, n + 3);
        }
    }

    private static float EdgeDistance(Vector2 direction, Vector2 half) =>
        Mathf.Min(half.x / Mathf.Max(0.000001f, Mathf.Abs(direction.x)),
            half.y / Mathf.Max(0.000001f, Mathf.Abs(direction.y)));
}
