using UnityEngine;
using UnityEngine.UI;

// A full cover that slides into a single diagonal result panel.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class EndscreenCurtain : MaskableGraphic
{
    public float Reveal { get; private set; }
    public bool OnRight { get; private set; }

    public void SetReveal(float value, bool onRight)
    {
        Reveal = Mathf.Clamp01(value);
        OnRight = onRight;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float w = r.width, h = r.height;
        float start = OnRight ? r.xMin - w * .15f : r.xMax + w * .15f;
        float end = r.center.x + (OnRight ? .02f : -.02f) * w;
        float x = Mathf.Lerp(start, end, Reveal);
        float skew = w * .065f;
        Vector2 bottom = new Vector2(x - skew, r.yMin);
        Vector2 top = new Vector2(x + skew, r.yMax);
        // Clamp the leading edge while it still lies beyond the screen.
        bottom.x = Mathf.Clamp(bottom.x, r.xMin, r.xMax);
        top.x = Mathf.Clamp(top.x, r.xMin, r.xMax);
        if (OnRight) Quad(vh, bottom, new Vector2(r.xMax,r.yMin), new Vector2(r.xMax,r.yMax), top, color);
        else Quad(vh, new Vector2(r.xMin,r.yMin), bottom, top, new Vector2(r.xMin,r.yMax), color);
        if (Reveal > .001f)
        {
            Vector2 normal = new Vector2(top.y-bottom.y, bottom.x-top.x).normalized * 2.5f;
            Quad(vh,bottom-normal,bottom+normal,top+normal,top-normal,Color.white);
        }
    }

    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int i=vh.currentVertCount;
        vh.AddVert(a,tint,Vector2.zero); vh.AddVert(b,tint,Vector2.zero);
        vh.AddVert(c,tint,Vector2.zero); vh.AddVert(d,tint,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
    }
}
