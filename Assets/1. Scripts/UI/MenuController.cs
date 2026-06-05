using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("메뉴 패널들")]
    public GameObject startMenu;         // 시작하기 버튼이 있는 묶음
    public GameObject levelSelectMenu;   // 레벨 1,2,3 버튼이 있는 묶음

    // ★ [핵심] 인트로를 보고 왔는지 기억하는 전역 변수
    public static bool isIntroDone = false;

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