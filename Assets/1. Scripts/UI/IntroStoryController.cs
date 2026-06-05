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

    [Header("교체할 마왕 일러스트")]
    public Sprite demonKingSprite;

    [Header("연출 속도 조절")]
    public float fadeDuration = 1.0f;   // 페이드 인/아웃 걸리는 시간 (1초)
    public float textSpeed = 0.05f;     // 한 글자씩 나오는 속도 (낮을수록 빠름)

    // 확정된 5문장 인트로 스토리 배열
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
        StartCoroutine(FadeIn());
        StartFirstDialogue();
    }

    void Update()
    {
        if (isFading) return;

        // 마우스 왼쪽 버튼을 클릭했을 때의 로직
        if (Input.GetMouseButtonDown(0))
        {
            // 1. 아직 글자가 출력 중일 때 클릭하면 ➡️ 한 번에 다 보여주기!
            if (isTypingComplete == false)
            {
                StopTypingAndShowFullText();
            }
            // 2. 글자가 이미 다 나온 상태에서 클릭하면 ➡️ 다음 대사로 넘어가기!
            else
            {
                NextDialogue();
            }
        }
    }

    void StartFirstDialogue()
    {
        currentIndex = 0;
        // 첫 번째 대사 타이핑 시작
        typingCoroutine = StartCoroutine(TypeText(dialogues[currentIndex]));
    }

    // 한 글자씩 또르륵 출력해주는 마법의 함수
    IEnumerator TypeText(string fullText)
    {
        isTypingComplete = false;
        storyText.text = ""; // 먼저 글 상자를 비웁니다.

        // [배경 교체 타이밍 수정] 
        // 3번째 대사("하지만... 정작 이 성의 주인인 나, 마왕은~")가 시작될 때 마왕 일러스트로 교체합니다.
        if (currentIndex == 2 && demonKingSprite != null)
        {
            backgroundImage.sprite = demonKingSprite;
        }

        // 문장 글자 수만큼 반복하며 한 글자씩 채우기
        for (int i = 0; i < fullText.Length; i++)
        {
            storyText.text += fullText[i];
            yield return new WaitForSeconds(textSpeed); // 설정한 속도만큼 대기
        }

        // 글자가 스스로 끝까지 다 나왔다면 완료 상태로 변경
        isTypingComplete = true;
    }

    // 글자가 나오는 도중 클릭했을 때 강제로 전체 대사를 보여주는 함수
    void StopTypingAndShowFullText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine); // 한 글자씩 찍던 코루틴을 강제로 중지!
        }

        storyText.text = dialogues[currentIndex]; // 전체 문장 한 번에 때려박기
        isTypingComplete = true; // 글자 출력 완료 상태로 변경
    }

    void NextDialogue()
    {
        currentIndex++;

        if (currentIndex < dialogues.Length)
        {
            // 다음 대사가 있다면 다시 한 글자씩 출력 시작
            typingCoroutine = StartCoroutine(TypeText(dialogues[currentIndex]));
        }
        else
        {
            // 모든 대사가 끝났다면 페이드 아웃 후 메인 메뉴로!
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