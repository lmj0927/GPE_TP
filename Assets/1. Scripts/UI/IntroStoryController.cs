using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class IntroStoryController : MonoBehaviour
{
    [Header("UI 연결 상자들")]
    public TextMeshProUGUI storyText;
    public Image backgroundImage;
    public Image fadePanel;

    [Header("스토리 일러스트들 (순서대로 등록)")]
    public Sprite storySprite1; // [대사 0, 1]에 쓰일 첫 번째 사진
    public Sprite storySprite2; // [대사 2] "하지만... 정작 이 성의 주인인 나" 에 쓰일 두 번째 사진
    public Sprite storySprite3; // [대사 3~5] "인간 세상을 너무 철저하게~" 부터 쓰일 세 번째 사진

    [Header("연출 속도 조절")]
    public float fadeDuration = 1.0f;   // 페이드 인/아웃 걸리는 시간 (1초)
    public float textSpeed = 0.05f;     // 한 글자씩 나오는 속도 (낮을수록 빠름)

    // 확정된 6문장 인트로 스토리 배열
    private string[] dialogues = new string[]
    {
        "\"인간들의 왕국이 모두 멸망하고, 대륙에는 오직 성검을 쥔 마지막 용사만이 남았다.\"",
        "\"녀석은 인류의 마지막 염원을 품고, 공주를 구하기 위해 이곳 마왕성의 탑으로 난입했다.\"",
        "\"하지만... 정작 이 성의 주인인 나, 마왕은 최상층 옥좌에 앉아 깊은 한숨을 쉬고 있다.\"",
        "\"인간 세상을 너무 철저하게 박살 낸 탓에, 마왕성에 자재를 납품해 줄 상단과 일꾼들까지 전부 전멸해 버린 것이다!\"",
        "\"그래도 용사만 막는다면 공주에게 건 저주가 완성되어 세상을 완전히 멸망시킬 것이다. 하하하하하하!\"",
        "\"끈질기게 기어 올라오는 불나방 같은 녀석... 절대로 이 위까지 올라오지마왕!\""
    };

    private int currentIndex = 0;
    private bool isFading = false;      // 페이드 중 클릭 방지 스위치

    private Coroutine typingCoroutine;  // 한 글자씩 출력하는 실시간 추적 장치
    private bool isTypingComplete = false; // 현재 대사가 다 출력되었는지 여부

    void Start()
    {
        AudioManager.TryPlay(AudioType.Intro);

        // 시작할 때 첫 번째 사진을 기본으로 깔아줍니다.
        if (storySprite1 != null)
        {
            backgroundImage.sprite = storySprite1;
        }

        StartCoroutine(FadeIn());
        StartFirstDialogue();
    }

    void Update()
    {
        if (isFading) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isTypingComplete == false)
            {
                StopTypingAndShowFullText();
            }
            else
            {
                NextDialogue();
            }
        }
    }

    void StartFirstDialogue()
    {
        currentIndex = 0;
        typingCoroutine = StartCoroutine(TypeText(dialogues[currentIndex]));
    }

    // 한 글자씩 또르륵 출력해주는 마법의 함수
    IEnumerator TypeText(string fullText)
    {
        isTypingComplete = false;
        storyText.text = ""; // 먼저 글 상자를 비웁니다.

        // [★ 대사 인덱스별 사진 실시간 교체 시스템]
        if (currentIndex == 2 && storySprite2 != null)
        {
            // 3번째 대사 ("하지만... 정작 이 성의 주인인 나~") 일 때 사진 2로 변경
            backgroundImage.sprite = storySprite2;
        }
        else if (currentIndex == 3 && storySprite3 != null)
        {
            // 4번째 대사 ("인간 세상을 너무 철저하게~") 일 때 사진 3으로 변경
            backgroundImage.sprite = storySprite3;
        }

        // 문장 글자 수만큼 반복하며 한 글자씩 채우기
        for (int i = 0; i < fullText.Length; i++)
        {
            storyText.text += fullText[i];
            yield return new WaitForSeconds(textSpeed);
        }

        isTypingComplete = true;
    }

    void StopTypingAndShowFullText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        storyText.text = dialogues[currentIndex];
        isTypingComplete = true;
    }

    void NextDialogue()
    {
        currentIndex++;

        if (currentIndex < dialogues.Length)
        {
            typingCoroutine = StartCoroutine(TypeText(dialogues[currentIndex]));
        }
        else
        {
            StartCoroutine(FadeOutAndLoadScene("MainMenu"));
        }
    }

    IEnumerator FadeIn()
    {
        isFading = true;
        float timer = 0f;
        Color color = fadePanel.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadePanel.color = color;
            yield return null;
        }

        color.a = 0f;
        fadePanel.color = color;
        fadePanel.gameObject.SetActive(false);
        isFading = false;
    }

    IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        isFading = true;
        fadePanel.gameObject.SetActive(true);
        float timer = 0f;
        Color color = fadePanel.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadePanel.color = color;
            yield return null;
        }

        color.a = 1f;
        fadePanel.color = color;

        MenuController.isIntroDone = true;
        SceneManager.LoadScene(sceneName);
    }
}