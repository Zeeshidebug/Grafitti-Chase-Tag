using UnityEngine;
using UnityEngine.UI;

// Screen-relative strips keep the leading edge diagonal at every aspect ratio.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuInkWipe : MaskableGraphic
{
    private float coverage;
    private static readonly Color32 Ink = new Color32(32, 9, 52, 255);
    private static readonly Color32 Violet = new Color32(91, 15, 136, 255);
    private static readonly Color32 Pink = new Color32(251, 33, 161, 255);
    private static readonly Color32 Cyan = new Color32(0, 201, 236, 255);

    public void SetCoverage(float value)
    {
        coverage = Mathf.Clamp01(value);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (coverage <= 0f) return;
        Rect r = rectTransform.rect;
        float x = Mathf.Lerp(-0.38f, 1.32f, coverage);
        Strip(mesh, r, x, 0.09f, 0f, 1f, Pink);
        Strip(mesh, r, x, 0.045f, 0f, 1f, Violet);
        Strip(mesh, r, x, 0f, 0f, 1f, Ink);
        Strip(mesh, r, x + 0.08f, 0.09f, 0.20f, 0.215f, Cyan, 0.02f);
        Strip(mesh, r, x + 0.09f, 0.12f, 0.73f, 0.735f, Pink, 0.04f);
    }

    private static void Strip(VertexHelper mesh, Rect rect, float x, float lead, float low, float high,
        Color32 color, float width = 2f)
    {
        float bottom = x + lead + (low - 0.5f) * 0.28f;
        float top = x + lead + (high - 0.5f) * 0.28f;
        int i = mesh.currentVertCount;
        mesh.AddVert(rect.min + Vector2.Scale(rect.size, new Vector2(bottom - width, low)), color, Vector2.zero);
        mesh.AddVert(rect.min + Vector2.Scale(rect.size, new Vector2(top - width, high)), color, Vector2.zero);
        mesh.AddVert(rect.min + Vector2.Scale(rect.size, new Vector2(top, high)), color, Vector2.zero);
        mesh.AddVert(rect.min + Vector2.Scale(rect.size, new Vector2(bottom, low)), color, Vector2.zero);
        mesh.AddTriangle(i, i + 1, i + 2);
        mesh.AddTriangle(i, i + 2, i + 3);
    }
}
