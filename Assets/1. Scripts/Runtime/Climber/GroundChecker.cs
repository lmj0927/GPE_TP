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
        float length = _config.GroundCheckBoxLength;
        float height = _config.GroundCheckBoxHeight;
        var size = new Vector2(length, height);
        var boxCenter = origin + Vector2.up * (height * 0.5f);

        var hit = Physics2D.BoxCast(
            boxCenter,
            size,
            0f,
            Vector2.down,
            height,
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

        float length = _config.GroundCheckBoxLength;
        float height = _config.GroundCheckBoxHeight;
        var size = new Vector3(length, height, 0f);
        var feet = _origin.position;
        var startCenter = feet + Vector3.up * (height * 0.5f);
        var endCenter = startCenter + Vector3.down * height;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(startCenter, size);
        Gizmos.DrawWireCube(endCenter, size);
        Gizmos.DrawLine(startCenter, endCenter);
    }
#endif
}
