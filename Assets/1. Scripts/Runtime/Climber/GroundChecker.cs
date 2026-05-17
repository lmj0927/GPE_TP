using UnityEngine;

public sealed class GroundChecker : MonoBehaviour
{
    [SerializeField] private Transform _origin;
    [SerializeField] private ClimberMovementConfig _config;

    public bool IsGrounded { get; private set; }

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
            return;
        }

        var hit = Physics2D.Raycast(
            _origin.position,
            Vector2.down,
            _config.GroundRayDistance,
            _config.GroundLayers);

        IsGrounded = hit.collider != null && !hit.collider.isTrigger;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_origin == null || _config == null)
            return;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        var start = _origin.position;
        var end = start + Vector3.down * _config.GroundRayDistance;
        Gizmos.DrawLine(start, end);
    }
#endif
}
