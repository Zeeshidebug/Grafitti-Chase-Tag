using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public Transform targetBone; 

    private void LateUpdate()
    {
        if (targetBone != null)
        {
            transform.position = targetBone.position;
        }
    }
}