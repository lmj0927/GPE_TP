using UnityEngine;

public sealed class RisingLava : MonoBehaviour
{
    [SerializeField] private float _riseSpeed = 0.2f;

    private Vector3 _startPosition;
    private bool _isRising;

    public float SurfaceY => transform.position.y;

    private void Awake()
    {
        CaptureStartPosition();
    }

    public void CaptureStartPosition()
    {
        _startPosition = transform.position;
    }

    public void ResetToStart()
    {
        transform.position = _startPosition;
        _isRising = true;
    }

    public void StopRising()
    {
        _isRising = false;
    }

    private void FixedUpdate()
    {
        if (!_isRising)
            return;

        var position = transform.position;
        position.y += _riseSpeed * Time.fixedDeltaTime;
        transform.position = position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IClimberAgent climber))
            climber.NotifyLavaContact();
    }
}
