using UnityEngine;

public sealed class GroundChecker : MonoBehaviour
{
    [SerializeField] private Transform _origin;
    [SerializeField] private ClimberMovementConfig _config;

    public bool IsGrounded { get; private set; }
    public Collider2D CurrentPlatform { get; private set; }

    public void Configure(Transform origin, ClimberMovementConfig config)
    {
        _origin = origin;
        _config = config;
    }

    public void Refresh()
    {
        if (_origin == null || _config == null)
        {
            IsGrounded = false;
            CurrentPlatform = null;
            return;
        }

        var origin = (Vector2)_origin.position;
        float radius = _config.GroundCheckRadius;
        float distance = _config.GroundRayDistance;

        var hit = Physics2D.CircleCast(
            origin,
            radius,
            Vector2.down,
            distance,
            _config.GroundLayers);

        if (hit.collider != null && !hit.collider.isTrigger)
        {
            IsGrounded = true;
            CurrentPlatform = hit.collider;
        }
        else
        {
            IsGrounded = false;
            CurrentPlatform = null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_origin == null || _config == null)
            return;

        float radius = _config.GroundCheckRadius;
        float distance = _config.GroundRayDistance;
        var start = _origin.position;
        var end = start + Vector3.down * distance;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(start, radius);
        Gizmos.DrawWireSphere(end, radius);
        Gizmos.DrawLine(start, end);
    }
#endif
}
