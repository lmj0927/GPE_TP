using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Events;

public class UpgradeUI : MonoBehaviour
{
    private const int MaxUpgradeLevel = 10;
    private const int PriceSeedA = 50;
    private const int PriceSeedB = 100;

    [Header("Animation")]
    [SerializeField] private float fadeTargetAlpha = 230f / 255f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float popupSlideOffsetY = 900f;
    [SerializeField] private float popupSlideDuration = 0.45f;
    [SerializeField] private float currencyPopDuration = 0.3f;
    [SerializeField] private float skillPopDuration = 0.25f;
    [SerializeField] private float skillPopStagger = 0.08f;
    [SerializeField] private float fillPopDuration = 0.2f;

    [Header("UI")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Image currencyImage;
    [SerializeField] private List<GameObject> skillTreeRoots;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private List<Button> upgradeButtons;
    [SerializeField] private List<TMP_Text> upgradeLevelTexts;
    [SerializeField] private List<TMP_Text> upgradePrices;
    [SerializeField] private List<Image> upgradeFillImages1;
    [SerializeField] private List<Image> upgradeFillImages2;
    [SerializeField] private List<Image> upgradeFillImages3;
    [SerializeField] private Button closeButton;

    private Sequence _showSequence;
    private Vector3 _popupRestPosition;
    private readonly List<UnityAction> _upgradeClickHandlers = new();

    private void Awake()
    {
        if (popupRoot != null)
            _popupRestPosition = popupRoot.transform.localPosition;

        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            int index = i;
            UnityAction action = () => OnUpgradeClicked(index);
            _upgradeClickHandlers.Add(action);
            if (upgradeButtons[i] != null)
                upgradeButtons[i].onClick.AddListener(action);
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        KillSequence();

        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            if (upgradeButtons[i] != null && i < _upgradeClickHandlers.Count)
                upgradeButtons[i].onClick.RemoveListener(_upgradeClickHandlers[i]);
        }

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }

    public void Show()
    {
        AudioManager.TryPlay(AudioType.Popup);
        KillSequence();
        gameObject.SetActive(true);
        SyncUI();
        PrepareShowState();

        _showSequence = DOTween.Sequence().SetUpdate(true);
        AppendFade(_showSequence);
        AppendPopupSlide(_showSequence);
        AppendCurrencyPop(_showSequence);
        AppendSkillTreesPop(_showSequence);
    }

    public void Hide()
    {
        KillSequence();
        gameObject.SetActive(false);
    }

    private void OnUpgradeClicked(int index)
    {
        var data = UserDataStore.Load();
        int previousLevel = GetUpgradeLevel(data, index);
        if (previousLevel >= MaxUpgradeLevel)
            return;

        int price = GetPriceForLevel(previousLevel + 1);
        if (data.currency < price)
            return;

        data.currency -= price;

        switch (index)
        {
            case 0:
                data.Upgrade1 = Mathf.Clamp(data.Upgrade1 + 1, 0, MaxUpgradeLevel);
                break;
            case 1:
                data.Upgrade2 = Mathf.Clamp(data.Upgrade2 + 1, 0, MaxUpgradeLevel);
                break;
            case 2:
                data.Upgrade3 = Mathf.Clamp(data.Upgrade3 + 1, 0, MaxUpgradeLevel);
                break;
            default:
                return;
        }

        int currentLevel = GetUpgradeLevel(data, index);
        UserDataStore.Save(data);
        SyncUI();

        AudioManager.TryPlay(AudioType.Plus);

        if (currentLevel > previousLevel)
            PlayFillPop(index, currentLevel - 1);
    }

    private void SyncUI()
    {
        var data = UserDataStore.Load();
        if (currencyText != null)
            currencyText.text = ": " + data.currency.ToString();

        ApplyUpgradeVisual(0, data.Upgrade1, upgradeFillImages1);
        ApplyUpgradeVisual(1, data.Upgrade2, upgradeFillImages2);
        ApplyUpgradeVisual(2, data.Upgrade3, upgradeFillImages3);
    }

    private void ApplyUpgradeVisual(int index, int level, List<Image> fillImages)
    {
        int clampedLevel = Mathf.Clamp(level, 0, MaxUpgradeLevel);

        if (index >= 0 && index < upgradeLevelTexts.Count && upgradeLevelTexts[index] != null)
            upgradeLevelTexts[index].text = clampedLevel >= MaxUpgradeLevel ? "Lv. MAX" : $"Lv. {clampedLevel} / {MaxUpgradeLevel}";

        if (index >= 0 && index < upgradePrices.Count && upgradePrices[index] != null)
        {
            upgradePrices[index].text = clampedLevel >= MaxUpgradeLevel
                ? "MAX"
                : GetPriceForLevel(clampedLevel + 1).ToString();
        }

        if (index >= 0 && index < upgradeButtons.Count && upgradeButtons[index] != null)
        {
            var data = UserDataStore.Load();
            int currentLevel = GetUpgradeLevel(data, index);
            int currentPrice = currentLevel >= MaxUpgradeLevel ? 0 : GetPriceForLevel(currentLevel + 1);
            upgradeButtons[index].interactable = currentLevel < MaxUpgradeLevel && data.currency >= currentPrice;
        }

        for (int i = 0; i < fillImages.Count; i++)
        {
            if (fillImages[i] != null)
                fillImages[i].gameObject.SetActive(i < clampedLevel);
        }
    }

    private void PrepareShowState()
    {
        if (fadeImage != null)
        {
            var color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }

        if (popupRoot != null)
            popupRoot.transform.localPosition = _popupRestPosition + Vector3.down * popupSlideOffsetY;

        if (currencyImage != null)
            currencyImage.rectTransform.localScale = new Vector3(1f, 0f, 1f);

        for (int i = 0; i < skillTreeRoots.Count; i++)
        {
            if (skillTreeRoots[i] != null)
                skillTreeRoots[i].transform.localScale = Vector3.zero;
        }
    }

    private void AppendFade(Sequence sequence)
    {
        if (fadeImage == null)
            return;

        sequence.Append(fadeImage.DOFade(fadeTargetAlpha, fadeDuration));
    }

    private void AppendPopupSlide(Sequence sequence)
    {
        if (popupRoot == null)
            return;

        sequence.Append(
            popupRoot.transform.DOLocalMoveY(_popupRestPosition.y, popupSlideDuration)
                .SetEase(Ease.OutCubic));
    }

    private void AppendCurrencyPop(Sequence sequence)
    {
        if (currencyImage == null)
            return;

        sequence.Append(
            currencyImage.rectTransform
                .DOScaleY(1f, currencyPopDuration)
                .SetEase(Ease.OutBack));
    }

    private void AppendSkillTreesPop(Sequence sequence)
    {
        for (int i = 0; i < skillTreeRoots.Count; i++)
        {
            var root = skillTreeRoots[i];
            if (root == null)
                continue;

            sequence.Append(
                root.transform.DOScale(Vector3.one, skillPopDuration).SetEase(Ease.OutBack));

            if (i < skillTreeRoots.Count - 1)
                sequence.AppendInterval(skillPopStagger);
        }
    }

    private void KillSequence()
    {
        if (_showSequence != null && _showSequence.IsActive())
            _showSequence.Kill();
        _showSequence = null;
    }

    private static int GetUpgradeLevel(UserData data, int index)
    {
        return index switch
        {
            0 => data.Upgrade1,
            1 => data.Upgrade2,
            2 => data.Upgrade3,
            _ => 0
        };
    }

    private static int GetPriceForLevel(int level)
    {
        if (level <= 1)
            return PriceSeedA;
        if (level == 2)
            return PriceSeedB;

        int prev2 = PriceSeedA;
        int prev1 = PriceSeedB;
        int current = prev1;
        for (int i = 3; i <= level; i++)
        {
            current = prev1 + prev2;
            prev2 = prev1;
            prev1 = current;
        }

        return current;
    }

    private void PlayFillPop(int upgradeIndex, int fillIndex)
    {
        List<Image> fillImages = upgradeIndex switch
        {
            0 => upgradeFillImages1,
            1 => upgradeFillImages2,
            2 => upgradeFillImages3,
            _ => null
        };

        if (fillImages == null || fillIndex < 0 || fillIndex >= fillImages.Count)
            return;

        Image target = fillImages[fillIndex];
        if (target == null)
            return;

        target.gameObject.SetActive(true);
        target.transform.localScale = Vector3.zero;
        target.transform.DOScale(Vector3.one, fillPopDuration).SetEase(Ease.OutBack);
    }
}
