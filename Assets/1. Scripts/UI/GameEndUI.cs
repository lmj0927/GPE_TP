using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameEndUI : MonoBehaviour
{
    private const float TimeSecondsSlow = 180f;
    private const float TimeSecondsFast = 60f;
    private const float TimeScoreAtSlow = 55f;
    private const float TimeScoreAtFast = 185f;
    private const float AccuracyAvg = 0.05f;
    private const float AccuracyHigh = 0.30f;
    private const float AccScoreAtAvg = 25f;
    private const float AccScoreAtHigh = 50f;

    [Header("Animation")]
    [SerializeField] private float _popupSlideOffsetY = 900f;
    [SerializeField] private float _popupSlideDuration = 0.45f;
    [SerializeField] private float _starPopDuration = 0.35f;
    [SerializeField] private float _starPopStagger = 0.1f;
    [SerializeField] private float _textTypingSpeed = 0.025f;

    [SerializeField] private RectTransform _gameEndPopUpUI;
    [SerializeField] private TMP_Text _gameResultText;
    [SerializeField] private TMP_Text _playTimeText;
    [SerializeField] private TMP_Text _spawnCountText;
    [SerializeField] private TMP_Text _hitCountText;
    [SerializeField] private Button _exitButton;
    [SerializeField] private List<Image> _starImages;
    [SerializeField] private List<float> _scorePerStar = new() { 70f, 190f };

    private Vector2 _popupRestAnchoredPosition;
    private Sequence _showSequence;
    private int _score;

    private void Awake()
    {
        if (_gameEndPopUpUI != null)
            _popupRestAnchoredPosition = _gameEndPopUpUI.anchoredPosition;

        _exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    private void OnDestroy()
    {
        KillShowSequence();
        _exitButton.onClick.RemoveListener(OnExitButtonClicked);
    }

    private void OnExitButtonClicked()
    {
        Time.timeScale = 1f;
        MenuController.ReturnFromGame();
        SceneManager.LoadScene("MainMenu");
    }

    public void Show(GameEndData gameEndData, Action<int> onWinStarsCalculated = null) =>
        ShowInternal(gameEndData, null, onWinStarsCalculated);

    /// <summary>Play-mode preview. <paramref name="starCount"/> is 1–3 on win, ignored on loss.</summary>
    public void ShowPreview(bool isWin, int starCount) =>
        ShowInternal(CreatePreviewData(isWin, starCount), isWin ? Mathf.Clamp(starCount, 1, 3) : 0, null);

    private static GameEndData CreatePreviewData(bool isWin, int starCount)
    {
        if (!isWin)
        {
            return new GameEndData
            {
                IsWin = false,
                PlayTimeSeconds = 120f,
                SpawnCount = 100,
                HitCount = 3
            };
        }

        return starCount switch
        {
            3 => new GameEndData { IsWin = true, PlayTimeSeconds = 60f, SpawnCount = 150, HitCount = 5 },
            2 => new GameEndData { IsWin = true, PlayTimeSeconds = 120f, SpawnCount = 80, HitCount = 5 },
            _ => new GameEndData { IsWin = true, PlayTimeSeconds = 180f, SpawnCount = 150, HitCount = 5 }
        };
    }

    private void ShowInternal(GameEndData gameEndData, int? forcedStarCount, Action<int> onWinStarsCalculated)
    {
        KillShowSequence();
        gameObject.SetActive(true);

        string playTimeLine = $"플레이 타임 : {gameEndData.PlayTimeSeconds:F2}초";
        string spawnLine = $"스폰 횟수 : {gameEndData.SpawnCount}";
        string hitLine = $"명중 횟수 : {gameEndData.HitCount}";

        _gameResultText.text = gameEndData.IsWin ? "승리" : "패배";
        SetTextEmpty(_playTimeText, _spawnCountText, _hitCountText);

        int stars = 0;
        if (gameEndData.IsWin)
        {
            if (forcedStarCount.HasValue)
            {
                stars = forcedStarCount.Value;
                _score = 0;
            }
            else
            {
                _score = CalculateScore(gameEndData);
                stars = CalculateStars(_score);
            }
        }

        if (gameEndData.IsWin && onWinStarsCalculated != null)
            onWinStarsCalculated.Invoke(stars);

        PrepareStarsForShow(stars);
        _showSequence = DOTween.Sequence().SetUpdate(true);

        AppendPopupSlide(_showSequence);

        if (gameEndData.IsWin)
            AppendStarPop(_showSequence, stars);

        AppendStatTextTyping(_showSequence, playTimeLine, spawnLine, hitLine);
    }

    private void AppendPopupSlide(Sequence sequence)
    {
        if (_gameEndPopUpUI == null)
            return;

        Vector2 offScreen = _popupRestAnchoredPosition + Vector2.down * _popupSlideOffsetY;
        _gameEndPopUpUI.anchoredPosition = offScreen;
        sequence.Append(
            _gameEndPopUpUI
                .DOAnchorPos(_popupRestAnchoredPosition, _popupSlideDuration)
                .SetEase(Ease.OutCubic));
    }

    private void AppendStarPop(Sequence sequence, int starCount)
    {
        if (_starImages == null || _starImages.Count == 0 || starCount <= 0)
            return;

        for (int i = 0; i < starCount; i++)
        {
            Image star = _starImages[i];
            if (star == null)
                continue;

            star.gameObject.SetActive(true);
            star.enabled = true;

            RectTransform starRect = star.rectTransform;
            starRect.localScale = Vector3.zero;

            sequence.Append(
                starRect
                    .DOScale(Vector3.one, _starPopDuration)
                    .SetEase(Ease.OutBack));

            if (i < starCount - 1)
                sequence.AppendInterval(_starPopStagger);
        }
    }

    private void AppendStatTextTyping(Sequence sequence, string playTimeLine, string spawnLine, string hitLine)
    {
        sequence.Append(CreateTypingTween(_playTimeText, playTimeLine));
        sequence.Append(CreateTypingTween(_spawnCountText, spawnLine));
        sequence.Append(CreateTypingTween(_hitCountText, hitLine));
    }

    private Tween CreateTypingTween(TMP_Text textComponent, string fullText)
    {
        if (textComponent == null)
            return null;

        textComponent.text = "";
        float duration = fullText.Length * _textTypingSpeed;

        return DOTween.To(() => 0, value =>
            {
                if (textComponent == null)
                    return;

                int length = Mathf.Clamp(value, 0, fullText.Length);
                textComponent.text = fullText.Substring(0, length);
            },
            fullText.Length,
            duration)
            .SetEase(Ease.Linear);
    }

    private void PrepareStarsForShow(int starCount)
    {
        if (_starImages == null)
            return;

        for (int i = 0; i < _starImages.Count; i++)
        {
            Image star = _starImages[i];
            if (star == null)
                continue;

            bool active = i < starCount;
            star.gameObject.SetActive(active);
            star.enabled = active;
            star.rectTransform.localScale = active ? Vector3.zero : Vector3.one;
        }
    }

    private static void SetTextEmpty(params TMP_Text[] texts)
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                texts[i].text = "";
        }
    }

    private void KillShowSequence()
    {
        if (_showSequence != null && _showSequence.IsActive())
            _showSequence.Kill();

        _showSequence = null;
    }

    private int CalculateScore(GameEndData data)
    {
        float accuracy = (float)data.HitCount / Mathf.Max(1, data.SpawnCount);

        float timeScore = Mathf.Lerp(
            TimeScoreAtSlow,
            TimeScoreAtFast,
            Mathf.InverseLerp(TimeSecondsSlow, TimeSecondsFast, data.PlayTimeSeconds));

        float accScore = Mathf.Lerp(
            AccScoreAtAvg,
            AccScoreAtHigh,
            Mathf.InverseLerp(AccuracyAvg, AccuracyHigh, accuracy));

        int score = Mathf.RoundToInt(timeScore + accScore);

        if (data.IsWin)
            return score;

        return Mathf.RoundToInt(score * 0.5f);
    }

    private int CalculateStars(int score)
    {
        int stars = 1;
        if (_scorePerStar.Count > 1 && score > _scorePerStar[1])
            stars = 3;
        else if (_scorePerStar.Count > 0 && score > _scorePerStar[0])
            stars = 2;

        return stars;
    }
}
