using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class HPBarUI : MonoBehaviour
{
    [SerializeField] private Image hpBar;
    [SerializeField] private Image hpBarBackground;
    [SerializeField] private GameAgent _gameAgent;
    [SerializeField, Min(0.01f)] private float _blinkHalfDuration = 0.06f;
    [SerializeField, Min(1)] private int _blinkCount = 3;
    [SerializeField, Min(0.01f)] private float _backgroundDrainDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _blinkWhiteLerp = 0.5f;

    private const float DecreaseHp = 0.2f;

    private int _lastHealth = -1;
    private Color _backgroundBaseColor;
    private Sequence _damageSequence;

    private void Awake()
    {
        if (hpBarBackground != null)
            _backgroundBaseColor = hpBarBackground.color;
    }

    private void Start()
    {
        if (_gameAgent == null)
            _gameAgent = FindFirstObjectByType<GameAgent>();

        SyncInstant(ReadNormalizedHealth(_gameAgent));
        _lastHealth = _gameAgent != null ? _gameAgent.Health : -1;
    }

    private void Update()
    {
        if (_gameAgent == null)
            return;

        int health = _gameAgent.Health;
        if (health == _lastHealth)
            return;

        float normalized = ReadNormalizedHealth(_gameAgent);

        if (_lastHealth < 0 || health > _lastHealth)
            SyncInstant(normalized);
        else
            PlayDamageEffect(health, normalized);

        _lastHealth = health;
    }

    private void OnDestroy() => KillDamageSequence();

    private static float ReadNormalizedHealth(GameAgent agent)
    {
        if (agent == null)
            return 0f;

        int maxHealth = agent.MaxHealth;
        return maxHealth > 0 ? (float)agent.Health / maxHealth : 0f;
    }

    private void SyncInstant(float normalized)
    {
        KillDamageSequence();
        SetFill(hpBar, normalized);
        SetFill(hpBarBackground, normalized);

        if (hpBarBackground != null)
            hpBarBackground.color = _backgroundBaseColor;
    }

    private void PlayDamageEffect(int health, float normalized)
    {
        float foregroundFill = health <= 0
            ? 0f
            : Mathf.Max(normalized, GetFill(hpBar) - DecreaseHp);

        SetFill(hpBar, foregroundFill);
        KillDamageSequence();

        if (hpBarBackground == null)
            return;

        Color blinkColor = Color.Lerp(_backgroundBaseColor, Color.white, _blinkWhiteLerp);
        blinkColor.a = _backgroundBaseColor.a;

        _damageSequence = DOTween.Sequence();

        for (int i = 0; i < _blinkCount; i++)
        {
            _damageSequence.Append(hpBarBackground.DOColor(blinkColor, _blinkHalfDuration));
            _damageSequence.Append(hpBarBackground.DOColor(_backgroundBaseColor, _blinkHalfDuration));
        }

        _damageSequence.Append(
            hpBarBackground
                .DOFillAmount(foregroundFill, _backgroundDrainDuration)
                .SetEase(Ease.Linear));

        _damageSequence.OnComplete(() =>
        {
            if (hpBarBackground != null)
                hpBarBackground.color = _backgroundBaseColor;
        });
    }

    private void KillDamageSequence()
    {
        if (_damageSequence != null && _damageSequence.IsActive())
            _damageSequence.Kill();

        _damageSequence = null;

        if (hpBarBackground != null)
            hpBarBackground.DOKill();
    }

    private static float GetFill(Image image) => image != null ? image.fillAmount : 0f;

    private static void SetFill(Image image, float fill)
    {
        if (image != null)
            image.fillAmount = Mathf.Clamp01(fill);
    }
}
