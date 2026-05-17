using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = new Vector3(0, target.position.y, target.position.z) + offset;
    }
}
