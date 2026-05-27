using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public float yOffset = 1.5f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(
            transform.position.x,
            target.position.y + yOffset,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );
    }
}
