using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class EnemyMarkerGraphic : MaskableGraphic
{
    [SerializeField] private Sprite sprite;
    [SerializeField, Range(0f, 0.2f)] private float inset;
    [SerializeField, Range(0.01f, 0.12f)] private float shoulderWidth = 0.055f;
    [SerializeField, Range(0f, 0.4f)] private float arrowLength = 0.19f;
    private readonly Vector2[] corners = {
        new Vector2(0.114f, 0.012f), new Vector2(0.841f, 0.132f),
        new Vector2(0.987f, 0.987f), new Vector2(0.012f, 0.796f)
    };
    private readonly Vector2[] rail = new Vector2[4];
    private readonly float[] distances = new float[5];
    private Vector2 direction = Vector2.down;
    private float opening = 1f;

    public override Texture mainTexture => sprite != null ? sprite.texture : s_WhiteTexture;
    public void SetSprite(Sprite value)
    {
        if (sprite == value) return;
        sprite = value;
        SetMaterialDirty();
        SetVerticesDirty();
    }

    public void SetArrow(Vector2 value, float amount)
    {
        if ((direction - value).sqrMagnitude < 0.000001f && Mathf.Abs(opening - amount) < 0.0001f) return;
        direction = value.normalized;
        opening = amount;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        float perimeter = 0f;
        for (int i = 0; i < 4; i++)
            rail[i] = rect.min + Vector2.Scale(rect.size, Vector2.Lerp(corners[i], Vector2.one * 0.5f, inset));
        for (int i = 0; i < 4; i++)
        {
            distances[i] = perimeter;
            perimeter += Vector2.Distance(rail[i], rail[(i + 1) % 4]);
        }
        distances[4] = perimeter;

        // The ray hits a continuous rail. Its two shoulders slide along that rail
        // when the middle vertex reaches an edge constraint, including corners.
        float tipDistance = 0f;
        Vector2 edgePoint = center;
        for (int i = 0; i < 4; i++)
        {
            Vector2 edge = rail[(i + 1) % 4] - rail[i];
            float denominator = Cross(direction, edge);
            if (Mathf.Abs(denominator) < 0.00001f) continue;
            Vector2 start = rail[i] - center;
            float alongRay = Cross(start, edge) / denominator;
            float alongEdge = Cross(start, direction) / denominator;
            if (alongRay < 0f || alongEdge < 0f || alongEdge > 1f) continue;
            tipDistance = distances[i] + edge.magnitude * alongEdge;
            edgePoint = center + direction * alongRay;
            break;
        }
        float half = perimeter * shoulderWidth;
        float end = tipDistance + half;
        float startDistance = tipDistance - half;
        Add(mesh, center, center, rect);
        Add(mesh, OnRail(end, perimeter), OnRail(end, perimeter), rect);
        for (int step = 1; step <= 4; step++)
        {
            // Traverse all remaining original corners in perimeter order.
            int nearest = -1;
            float nextDistance = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float wrapped = end + Mathf.Repeat(distances[i] - end, perimeter);
                if (wrapped > end + 0.001f && wrapped < nextDistance)
                {
                    nextDistance = wrapped;
                    nearest = i;
                }
            }
            if (nearest < 0 || nextDistance >= startDistance + perimeter) break;
            Add(mesh, rail[nearest], rail[nearest], rect);
            end = nextDistance + 0.001f;
        }
        Vector2 shoulder = OnRail(startDistance, perimeter);
        Add(mesh, shoulder, shoulder, rect);
        // Clamp UV to the colored edge while stretching its geometry into a tip.
        Vector2 tip = edgePoint + direction * (rect.height * arrowLength * opening);
        Add(mesh, tip, Vector2.Lerp(edgePoint, center, 0.035f), rect);
        int count = mesh.currentVertCount;
        // Convex rail plus one radial wedge remains star shaped about the center.
        for (int i = 1; i < count; i++) mesh.AddTriangle(0, i, i + 1 < count ? i + 1 : 1);
    }

    private Vector2 OnRail(float distance, float perimeter)
    {
        distance = Mathf.Repeat(distance, perimeter);
        for (int i = 0; i < 4; i++)
            if (distance <= distances[i + 1])
                return Vector2.Lerp(rail[i], rail[(i + 1) % 4], (distance - distances[i]) / (distances[i + 1] - distances[i]));
        return rail[0];
    }

    private void Add(VertexHelper mesh, Vector2 position, Vector2 texturePosition, Rect rect)
    {
        Vector4 uv = sprite != null ? DataUtility.GetOuterUV(sprite) : new Vector4(0f, 0f, 1f, 1f);
        Vector2 normalized = (texturePosition - rect.min) / rect.size;
        mesh.AddVert(position, color, new Vector2(Mathf.Lerp(uv.x, uv.z, normalized.x), Mathf.Lerp(uv.y, uv.w, normalized.y)));
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
