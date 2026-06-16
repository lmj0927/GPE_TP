using DG.Tweening;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private GameAgent target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private MapGenerator _mapGenerator;
    [SerializeField, Min(0.01f)] private float _followSmoothTime = 0.12f;
    private Vector3 _hitShakeStrength = new(0.05f, 0.05f, 0f);
    private int _hitShakeVibrato = 1;
    [SerializeField, Min(0.01f)] private float _hitShakeDuration = 0.1f;

    private Vector3 _followVelocity;
    private Vector3 _shakeOffset;
    private Vector3 _targetWorldPosition;
    private bool _hasTargetPosition;
    private Tween _shakeTween;

    private void Awake()
    {
        if (_mapGenerator == null)
            _mapGenerator = FindFirstObjectByType<MapGenerator>();
        ResolveTarget();
    }

    private void OnEnable()
    {
        TrySubscribeTarget();
    }

    private void OnDisable()
    {
        UnsubscribeTarget();
    }

    private void LateUpdate()
    {
        if (!_hasTargetPosition)
            return;

        var targetPosition = new Vector3(0f, _targetWorldPosition.y, _targetWorldPosition.z) + offset;

        if (_mapGenerator != null && _mapGenerator.HasEndTemplateWorldY)
            targetPosition.y = Mathf.Min(targetPosition.y, _mapGenerator.EndTemplateWorldY);

        Vector3 smoothed = Vector3.SmoothDamp(transform.position, targetPosition, ref _followVelocity, _followSmoothTime);
        transform.position = smoothed + _shakeOffset;
    }

    public void PlayHitShake()
    {
        if (_shakeTween != null && _shakeTween.IsActive())
            _shakeTween.Kill();

        _shakeOffset = Vector3.zero;
        _shakeTween = DOTween.Shake(
                () => _shakeOffset,
                value => _shakeOffset = value,
                _hitShakeDuration,
                _hitShakeStrength,
                _hitShakeVibrato)
            .SetUpdate(UpdateType.Late)
            .OnComplete(() => _shakeOffset = Vector3.zero);
    }

    private void ResolveTarget()
    {
        if (target == null)
            target = FindFirstObjectByType<GameAgent>();
    }

    private void TrySubscribeTarget()
    {
        ResolveTarget();
        if (target == null)
            return;

        target.PositionUpdated += OnTargetPositionUpdated;
        target.HitTaken += OnTargetHitTaken;
        OnTargetPositionUpdated(target.transform.position);
    }

    private void UnsubscribeTarget()
    {
        if (target == null)
            return;

        target.PositionUpdated -= OnTargetPositionUpdated;
        target.HitTaken -= OnTargetHitTaken;
    }

    private void OnTargetPositionUpdated(Vector3 worldPosition)
    {
        _targetWorldPosition = worldPosition;
        _hasTargetPosition = true;
    }

    private void OnTargetHitTaken()
    {
        PlayHitShake();
    }

    private void OnDestroy()
    {
        UnsubscribeTarget();

        if (_shakeTween != null && _shakeTween.IsActive())
            _shakeTween.Kill();
    }
}
