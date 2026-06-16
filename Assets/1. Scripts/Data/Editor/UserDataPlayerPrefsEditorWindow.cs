#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class UserDataPlayerPrefsEditorWindow : EditorWindow
{
    private UserData _userData = new();
    private Vector2 _scrollPosition;

    [MenuItem("Tools/User Data PlayerPrefs")]
    public static void Open()
    {
        var window = GetWindow<UserDataPlayerPrefsEditorWindow>("UserData Prefs");
        window.minSize = new Vector2(420f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        ReloadUserData();
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.LabelField("Level Progress", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "IsFirstEntry: false=최초 진입(스토리 표시), true=재진입(스토리 생략). Star: 0–3 (1 이상이면 클리어).",
            MessageType.Info);

        DrawLevelProgress(1, _userData.Level1);
        DrawLevelProgress(2, _userData.Level2);
        DrawLevelProgress(3, _userData.Level3);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Upgrade / Currency", EditorStyles.boldLabel);
        DrawUpgradeAndCurrency();

        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload"))
                ReloadUserData();

            if (GUILayout.Button("Save Level Progress"))
                SaveUserData();

            if (GUILayout.Button("Clear All UserData Keys"))
            {
                if (EditorUtility.DisplayDialog(
                        "Clear UserData",
                        "UserData 관리 키(레벨당 3개 × 3)를 모두 삭제할까요?",
                        "Delete",
                        "Cancel"))
                {
                    UserDataStore.DeleteAllUserDataKeys();
                    ReloadUserData();
                }
            }
        }

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Raw Keys", EditorStyles.boldLabel);
        DrawRawKeyRow(UserDataPrefsKeys.LevelFirstEntry(1));
        DrawStarKeyRow(UserDataPrefsKeys.LevelStar(1));
        DrawRawKeyRow(UserDataPrefsKeys.LevelFirstEntry(2));
        DrawStarKeyRow(UserDataPrefsKeys.LevelStar(2));
        DrawRawKeyRow(UserDataPrefsKeys.LevelFirstEntry(3));
        DrawStarKeyRow(UserDataPrefsKeys.LevelStar(3));
        DrawLevelKeyRow(UserDataPrefsKeys.Upgrade1, 10);
        DrawLevelKeyRow(UserDataPrefsKeys.Upgrade2, 10);
        DrawLevelKeyRow(UserDataPrefsKeys.Upgrade3, 10);
        DrawCurrencyKeyRow(UserDataPrefsKeys.Currency);

        EditorGUILayout.EndScrollView();
    }

    private void DrawLevelProgress(int level, LevelProgress progress)
    {
        EditorGUILayout.LabelField($"Level {level}", EditorStyles.miniBoldLabel);

        using (new EditorGUI.IndentLevelScope())
        {
            progress.IsFirstEntry = EditorGUILayout.Toggle("Is First Entry (seen)", progress.IsFirstEntry);
            progress.Star = EditorGUILayout.IntSlider("Stars", progress.Star, 0, 3);
            EditorGUILayout.LabelField("Cleared (derived)", progress.IsCleared ? "True" : "False");
        }
    }

    private void DrawRawKeyRow(string key)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(key, GUILayout.Width(220f));
            EditorGUILayout.LabelField(DescribeStoredValue(key));

            if (GUILayout.Button("0", GUILayout.Width(24f)))
            {
                PlayerPrefs.SetInt(key, 0);
                PlayerPrefs.Save();
                ReloadUserData();
            }

            if (GUILayout.Button("1", GUILayout.Width(24f)))
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                ReloadUserData();
            }

            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                UserDataStore.DeleteKey(key);
                PlayerPrefs.Save();
                ReloadUserData();
            }
        }
    }

    private void DrawStarKeyRow(string key)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(key, GUILayout.Width(220f));
            EditorGUILayout.LabelField(DescribeStoredValue(key));

            for (int star = 0; star <= 3; star++)
            {
                int value = star;
                if (GUILayout.Button(value.ToString(), GUILayout.Width(24f)))
                {
                    PlayerPrefs.SetInt(key, value);
                    PlayerPrefs.Save();
                    ReloadUserData();
                }
            }

            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                UserDataStore.DeleteKey(key);
                PlayerPrefs.Save();
                ReloadUserData();
            }
        }
    }

    private void DrawLevelKeyRow(string key, int maxLevel)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(key, GUILayout.Width(220f));
            EditorGUILayout.LabelField(DescribeStoredValue(key));

            for (int level = 0; level <= maxLevel; level++)
            {
                int value = level;
                if (GUILayout.Button(value.ToString(), GUILayout.Width(24f)))
                {
                    PlayerPrefs.SetInt(key, value);
                    PlayerPrefs.Save();
                    ReloadUserData();
                }
            }

            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                UserDataStore.DeleteKey(key);
                PlayerPrefs.Save();
                ReloadUserData();
            }
        }
    }

    private void DrawCurrencyKeyRow(string key)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(key, GUILayout.Width(220f));
            EditorGUILayout.LabelField(DescribeStoredValue(key));

            if (GUILayout.Button("0", GUILayout.Width(28f)))
                SetCurrencyValue(key, 0);

            if (GUILayout.Button("1k", GUILayout.Width(36f)))
                SetCurrencyValue(key, 1000);

            if (GUILayout.Button("10k", GUILayout.Width(40f)))
                SetCurrencyValue(key, 10000);

            if (GUILayout.Button("X", GUILayout.Width(24f)))
            {
                UserDataStore.DeleteKey(key);
                PlayerPrefs.Save();
                ReloadUserData();
            }
        }
    }

    private void DrawUpgradeAndCurrency()
    {
        using (new EditorGUI.IndentLevelScope())
        {
            _userData.Upgrade1 = EditorGUILayout.IntSlider("Upgrade1", _userData.Upgrade1, 0, 10);
            _userData.Upgrade2 = EditorGUILayout.IntSlider("Upgrade2", _userData.Upgrade2, 0, 10);
            _userData.Upgrade3 = EditorGUILayout.IntSlider("Upgrade3", _userData.Upgrade3, 0, 10);
            _userData.currency = EditorGUILayout.IntField("Currency", Mathf.Max(0, _userData.currency));
        }
    }

    private void SetCurrencyValue(string key, int value)
    {
        PlayerPrefs.SetInt(key, Mathf.Max(0, value));
        PlayerPrefs.Save();
        ReloadUserData();
    }

    private static string DescribeStoredValue(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return "(missing)";

        return $"int={PlayerPrefs.GetInt(key)}";
    }

    private void ReloadUserData()
    {
        _userData = UserDataStore.Load();
        Repaint();
    }

    private void SaveUserData()
    {
        UserDataStore.Save(_userData);
        ReloadUserData();
    }
}
#endif
