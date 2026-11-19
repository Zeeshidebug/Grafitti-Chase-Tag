using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public Transform targetBone;
    [Tooltip("Fallback bone path relative to the character Animator when the scene reference is missing.")]
    [SerializeField] private string playerRelativePath;

    public static void Bind(Component emitter, Transform point, Animator model)
    {
        if (emitter == null) return;
        var follower = emitter.GetComponent<FollowTarget>();
        if (point == null && follower == null) return;
        if (follower == null) follower = emitter.gameObject.AddComponent<FollowTarget>();
        if (point != null) follower.targetBone = point;
        else if (follower.targetBone == null && model != null && !string.IsNullOrEmpty(follower.playerRelativePath))
            follower.targetBone = model.transform.Find(follower.playerRelativePath);

        // Snap before the first emission. Only position follows the animated
        // bone so authored dust/mesh orientations stay relative to the player.
        follower.Follow();
    }

    private void LateUpdate() => Follow();

    private void Follow()
    {
        if (targetBone != null && !targetBone.IsChildOf(transform))
        {
            transform.position = targetBone.position;
        }
    }
}
