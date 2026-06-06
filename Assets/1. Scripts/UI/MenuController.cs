using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("메뉴 패널들")]
    public GameObject startMenu;         // 시작하기 버튼이 있는 묶음
    public GameObject levelSelectMenu;   // 레벨 1,2,3 버튼이 있는 묶음

    [Header("레벨 버튼 (비어 있으면 Level1/2/3 자식에서 자동 탐색)")]
    [SerializeField] private Button _level1Button;
    [SerializeField] private Button _level2Button;
    [SerializeField] private Button _level3Button;

    // ★ [핵심] 인트로를 보고 왔는지 기억하는 전역 변수
    public static bool isIntroDone = false;

    private void OnEnable()
    {
        ResolveLevelButtons();
        ApplyLevelButtonStates();
    }

    void Start()
    {
        // 처음 시작할 때(isIntroDone이 false일 때)
        if (isIntroDone == false)
        {
            startMenu.SetActive(true);      // START 버튼 보이기
            levelSelectMenu.SetActive(false); // 레벨 선택창 숨기기
        }
        else // 인트로를 보고 왔을 때(isIntroDone이 true일 때)
        {
            startMenu.SetActive(false);     // START 버튼 숨기기
            levelSelectMenu.SetActive(true);  // 레벨 선택창 바로 보이기
        }

        ApplyLevelButtonStates();
    }

    public static void ReturnFromGame()
    {
        isIntroDone = true;
    }

    private void ResolveLevelButtons()
    {
        if (levelSelectMenu == null)
            return;

        Transform root = levelSelectMenu.transform;

        if (_level1Button == null)
            _level1Button = root.Find("Level1")?.GetComponent<Button>();

        if (_level2Button == null)
            _level2Button = root.Find("Level2")?.GetComponent<Button>();

        if (_level3Button == null)
            _level3Button = root.Find("Level3")?.GetComponent<Button>();
    }

    private void ApplyLevelButtonStates()
    {
        UserData userData = UserDataStore.Load();

        SetButtonInteractable(_level1Button, true);
        SetButtonInteractable(_level2Button, userData.Level1.IsCleared);
        SetButtonInteractable(_level3Button, userData.Level2.IsCleared);

        ApplyLevelStars(_level1Button, userData.Level1.Star);
        ApplyLevelStars(_level2Button, userData.Level2.Star);
        ApplyLevelStars(_level3Button, userData.Level3.Star);
    }

    private static void ApplyLevelStars(Button levelButton, int starCount)
    {
        if (levelButton == null)
            return;

        Transform starsRoot = levelButton.transform.Find("Stars");
        if (starsRoot == null)
            return;

        int activeCount = Mathf.Clamp(starCount, 0, starsRoot.childCount);
        for (int i = 0; i < starsRoot.childCount; i++)
        {
            Transform filledStar = starsRoot.GetChild(i).Find("Star");
            if (filledStar != null)
                filledStar.gameObject.SetActive(i < activeCount);
        }
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    // START 버튼에 연결할 함수 (기존 로직 유지)
    public void LoadIntro()
    {
        SceneManager.LoadScene("IntroStory");
    }

    // 레벨 버튼에 연결할 함수 (기존 함수를 아래처럼 수정!)
    public void LoadLevel(string sceneName)
    {
        // [★핵심 추가] 게임 한 판이 시작되는 것이므로, 
        // 다음에 메인메뉴로 돌아왔을 때는 다시 타이틀(START 버튼)이 뜨도록 상태를 리셋
        isIntroDone = false;

        SceneManager.LoadScene(sceneName);
    }
}