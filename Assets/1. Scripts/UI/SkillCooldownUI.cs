using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillCooldownUI : MonoBehaviour
{
    [SerializeField] private Image _slotImage;
    [SerializeField, Range(0f, 1f)] private float _pulseWhiteLerp = 0.3f;
    [SerializeField, Min(0.01f)] private float _selectEnterDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float _deselectDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float _pulseDuration = 0.55f;

    public Image overlay;
    public PlayerObstacleSpawner spawner;
    public ObstacleKind obstacleKind;

    private Color _originalSlotColor;
    private Sequence _selectionSequence;
    private bool _isSelected;

    private void Awake()
    {
        ResolveSlotImage();

        if (_slotImage != null)
            _originalSlotColor = _slotImage.color;
    }

    private void Start()
    {
        if (_slotImage != null && _originalSlotColor.a <= 0f)
            _originalSlotColor = _slotImage.color;
    }

    private void Update()
    {
        if (overlay == null || spawner == null)
            return;

        float duration = spawner.CooldownDuration(obstacleKind);
        float remain = spawner.RemainingCooldown(obstacleKind);

        if (duration <= 0f)
        {
            overlay.fillAmount = 0f;
            return;
        }

        overlay.fillAmount = remain / duration;
    }

    private void OnDestroy()
    {
        KillSelectionTweens();
    }

    public void SetSelected(bool selected)
    {
        if (_slotImage == null || _isSelected == selected)
            return;

        _isSelected = selected;
        KillSelectionTweens();

        if (!selected)
        {
            _slotImage
                .DOColor(_originalSlotColor, _deselectDuration)
                .SetEase(Ease.OutQuad);
            return;
        }

        Color peak = Color.Lerp(_originalSlotColor, Color.white, _pulseWhiteLerp);
        peak.a = _originalSlotColor.a;

        _selectionSequence = DOTween.Sequence()
            .Append(_slotImage.DOColor(peak, _selectEnterDuration).SetEase(Ease.OutCubic))
            .OnComplete(StartSelectionPulse);
    }

    private void StartSelectionPulse()
    {
        if (!_isSelected || _slotImage == null)
            return;

        _slotImage
            .DOColor(_originalSlotColor, _pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void KillSelectionTweens()
    {
        if (_selectionSequence != null && _selectionSequence.IsActive())
            _selectionSequence.Kill();

        _selectionSequence = null;

        if (_slotImage != null)
            _slotImage.DOKill();
    }

    private void ResolveSlotImage()
    {
        if (_slotImage != null)
            return;

        var images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            var image = images[i];
            if (image == overlay)
                continue;

            if (image.name.StartsWith("SkillSlot"))
            {
                _slotImage = image;
                return;
            }
        }
    }
}
