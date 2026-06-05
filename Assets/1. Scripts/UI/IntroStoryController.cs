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

    private string[] dialogues = new string[]
    {
     // 1. 첫 번째 자막 (핏빛 배경에서 시작)
     "하늘이 핏빛으로 물들고, 대지에 심연의 구멍이 뚫렸다. 마왕이 공주를 제물 삼아 대의식을 시작했기 때문이다. 나라의 모든 군대가 전멸한 가운데, 오직 한 사람... 성검을 쥔 용사만이 공주를 구하고 저주를 끊기 위해 마왕성의 탑으로 진입했다.", 
     "(마왕은 최상층 옥좌에서 비웃으며 아래를 내려다본다.)", 
     // 3. 세 번째 자막 (또 클릭하면 나오는 마지막 자막)
     "\"의식이 완성될 때까지, 저 불나방 같은 침입자의 숨통을 끊어놓아라.\""
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

        // 마우스 왼쪽 버튼을 클릭했을 때의 로직 (팀장님 스타일 완성)
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

        // 배경 교체 타이밍 체크 (팀장님 로직 유지)
        if (currentIndex == 1 && demonKingSprite != null)
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