using UnityEngine;

public sealed class RisingLava : MonoBehaviour
{
    [SerializeField] private float _riseSpeed = 0.2f;

    private Vector3 _startPosition;

    public float SurfaceY => transform.position.y;

    private void Awake()
    {
        _startPosition = transform.position;
    }

    public void ResetToStart()
    {
        transform.position = _startPosition;
    }

    private void FixedUpdate()
    {
        var position = transform.position;
        position.y += _riseSpeed * Time.fixedDeltaTime;
        transform.position = position;
    }
}
