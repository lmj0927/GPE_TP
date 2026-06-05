#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (GameManager)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("End UI Preview", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play 모드에서 종료 UI 연출을 미리 볼 수 있습니다. 승리 버튼은 UserData 클리어도 저장합니다.",
                    MessageType.Info);
            }

            if (GUILayout.Button("3성 승리", GUILayout.Height(28)))
                manager.PreviewEndThreeStarWin();

            if (GUILayout.Button("2성 승리", GUILayout.Height(28)))
                manager.PreviewEndTwoStarWin();

            if (GUILayout.Button("1성 승리", GUILayout.Height(28)))
                manager.PreviewEndOneStarWin();

            if (GUILayout.Button("패배", GUILayout.Height(28)))
                manager.PreviewEndLose();
        }
    }
}
#endif
