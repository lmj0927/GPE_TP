using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-2000)]
public class StoryUI : MonoBehaviour
{
    [SerializeField] private UI_TextDialog storyDialog;
    [SerializeField] private Sprite characterSprite;
    [SerializeField] private Sprite princessSprite;
    [SerializeField] private Sprite bossSprite;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image bossImage;
    [SerializeField] private List<StoryData> storyData;

    private int _currentIndex;
    private Coroutine _showCoroutine;

    public void Show()
    {
        if (storyData == null || storyData.Count == 0)
            return;

        if (storyDialog == null)
            return;

        gameObject.SetActive(true);

        if (_showCoroutine != null)
            StopCoroutine(_showCoroutine);

        _showCoroutine = StartCoroutine(ShowAfterLayout());
    }

    private IEnumerator ShowAfterLayout()
    {
        yield return new WaitForEndOfFrame();

        if (storyDialog != null)
            storyDialog.gameObject.SetActive(true);

        if (characterImage != null)
            characterImage.gameObject.SetActive(false);

        if (bossImage != null)
            bossImage.gameObject.SetActive(false);

        storyDialog.SetAutoHideOnClick(false);
        BindDialogClick();

        _currentIndex = 0;
        Time.timeScale = 0f;
        DisplayLine(_currentIndex);
        _showCoroutine = null;
    }
    public void Hide()
    {
        UnbindDialogClick();

        if (storyDialog != null)
            storyDialog.Hide();

        if (characterImage != null)
            characterImage.gameObject.SetActive(false);

        if (bossImage != null)
            bossImage.gameObject.SetActive(false);

        Time.timeScale = 1;
    }

    private void BindDialogClick()
    {
        if (storyDialog == null)
            return;

        storyDialog.OnTextBalloonClicked -= OnDialogClicked;
        storyDialog.OnTextBalloonClicked += OnDialogClicked;
    }

    private void UnbindDialogClick()
    {
        if (storyDialog == null)
            return;

        storyDialog.OnTextBalloonClicked -= OnDialogClicked;
    }

    private void OnDialogClicked(string _)
    {
        _currentIndex++;

        if (_currentIndex >= storyData.Count)
        {
            Hide();
            return;
        }

        DisplayLine(_currentIndex);
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    private void DisplayLine(int index)
    {
        StoryData data = storyData[index];
        ApplyPortrait(data.characterType);

        if (storyDialog != null && !string.IsNullOrEmpty(data.text))
            storyDialog.ShowText(data.text);
    }

    private void ApplyPortrait(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.Boss:
                if (characterImage != null)
                    characterImage.gameObject.SetActive(false);

                if (bossImage != null)
                {
                    bossImage.gameObject.SetActive(true);
                    if (bossSprite != null)
                        bossImage.sprite = bossSprite;
                }
                break;

            case CharacterType.Princess:
                if (bossImage != null)
                    bossImage.gameObject.SetActive(false);

                if (characterImage != null)
                {
                    characterImage.gameObject.SetActive(true);
                    if (princessSprite != null)
                        characterImage.sprite = princessSprite;
                }
                break;

            default:
                if (bossImage != null)
                    bossImage.gameObject.SetActive(false);

                if (characterImage != null)
                {
                    characterImage.gameObject.SetActive(true);
                    if (characterSprite != null)
                        characterImage.sprite = characterSprite;
                }
                break;
        }
    }
}

[System.Serializable]
public class StoryData
{
    public CharacterType characterType;
    public string text;
}

public enum CharacterType
{
    Character,
    Princess,
    Boss
}
