using UnityEngine;
using UnityEngine.UI;

// One screen-space cover: impact ring, expanding disk, then diagonal split.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RoundResultCover : MaskableGraphic
{
    public float Circle { get; private set; }
    public float Opening { get; private set; }
    private float wave, waveAlpha, cut;
    private readonly Vector2[] clipped = new Vector2[8];
    public void Set(float circle, float opening = 0f, float ring = 0f, float ringAlpha = 0f, float cutProgress = 0f)
    {
        Circle = circle; Opening = opening; wave = ring; waveAlpha = ringAlpha; cut = cutProgress;
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Vector2 h = rectTransform.rect.size * .5f;
        if (h.x <= 0 || h.y <= 0) return;
        if (Circle < 1f)
        {
            if (waveAlpha > 0) Ring(vh, h.magnitude * wave, 14f, new Color(color.r, color.g, color.b, waveAlpha));
            if (Circle > 0) Ring(vh, (h.magnitude + 8f) * Circle, (h.magnitude + 8f) * Circle, color);
            return;
        }
        float slope = .85f;
        float gap = (h.x + slope * h.y + 16f) * Opening;
        if (Opening < 1f)
        {
            Polygon(vh, h, slope, gap, true, color);
            Polygon(vh, h, slope, -gap, false, color);
            if (Opening > 0)
            {
                Edge(vh, h, slope, gap, Color.white);
                Edge(vh, h, slope, -gap, Color.white);
            }
        }
        if (cut > 0 && Opening == 0)
        {
            float y = Mathf.Min(h.y, h.x / slope);
            Vector2 top = new Vector2(slope * y, y);
            Line(vh, top, Vector2.Lerp(top, -top, cut), 7f, Color.white);
        }
    }
    private void Ring(VertexHelper vh, float radius, float thickness, Color tint)
    {
        const int count = 192;
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count, b = (i + 1) * Mathf.PI * 2f / count;
            Vector2 da = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Quad(vh, da * radius, db * radius, db * Mathf.Max(0, radius - thickness), da * Mathf.Max(0, radius - thickness), tint);
        }
    }
    private void Polygon(VertexHelper vh, Vector2 h, float slope, float boundary, bool greater, Color tint)
    {
        int n = 0;
        Vector2 prev = new Vector2(-h.x, h.y);
        float pd = (prev.x - slope * prev.y - boundary) * (greater ? 1 : -1);
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = i == 0 ? new Vector2(-h.x, -h.y) : i == 1 ? new Vector2(h.x, -h.y) : i == 2 ? h : new Vector2(-h.x, h.y);
            float d = (p.x - slope * p.y - boundary) * (greater ? 1 : -1);
            if ((d >= 0) != (pd >= 0)) clipped[n++] = Vector2.Lerp(prev, p, pd / (pd - d));
            if (d >= 0) clipped[n++] = p;
            prev = p; pd = d;
        }
        int start = vh.currentVertCount;
        for (int i = 0; i < n; i++) vh.AddVert(clipped[i], tint, Vector2.zero);
        for (int i = 1; i < n - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
    }
    private void Edge(VertexHelper vh, Vector2 h, float slope, float b, Color tint)
    {
        float lo = Mathf.Max(-h.y, (-h.x - b) / slope), hi = Mathf.Min(h.y, (h.x - b) / slope);
        if (lo < hi) Line(vh, new Vector2(slope * lo + b, lo), new Vector2(slope * hi + b, hi), 6f, tint);
    }
    private void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 d = (b - a).normalized; Vector2 n = new Vector2(-d.y, d.x) * width * .5f;
        Quad(vh, a + n, b + n, b - n, a - n, tint);
    }
    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int i = vh.currentVertCount; vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.zero); vh.AddVert(c, tint, Vector2.zero); vh.AddVert(d, tint, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }
}
