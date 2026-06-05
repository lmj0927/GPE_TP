#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UserDataPlayerPrefsEditorWindow : EditorWindow
{
    private const string CustomKeysEditorPrefsKey = "UserDataPlayerPrefsEditorWindow_CustomKeys";

    private UserData _userData = new();
    private string _customKey = string.Empty;
    private bool _customValue;
    private UserDataBoolStorageFormat _customFormat = UserDataBoolStorageFormat.IntZeroOne;
    private readonly List<string> _trackedCustomKeys = new();
    private Vector2 _scrollPosition;

    [MenuItem("Tools/User Data PlayerPrefs")]
    public static void Open()
    {
        var window = GetWindow<UserDataPlayerPrefsEditorWindow>("UserData Prefs");
        window.minSize = new Vector2(420f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        ReloadUserData();
        LoadTrackedCustomKeys();
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.LabelField("Level Progress", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "IsFirstEntry: false=최초 진입(스토리 표시), true=재진입(스토리 생략). Int 0/1 저장.",
            MessageType.Info);

        DrawLevelProgress(1, _userData.Level1);
        DrawLevelProgress(2, _userData.Level2);
        DrawLevelProgress(3, _userData.Level3);

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
        DrawRawKeyRow(UserDataPrefsKeys.LevelCleared(1));
        DrawRawKeyRow(UserDataPrefsKeys.LevelThreeStar(1));
        DrawRawKeyRow(UserDataPrefsKeys.LevelFirstEntry(2));
        DrawRawKeyRow(UserDataPrefsKeys.LevelCleared(2));
        DrawRawKeyRow(UserDataPrefsKeys.LevelThreeStar(2));
        DrawRawKeyRow(UserDataPrefsKeys.LevelFirstEntry(3));
        DrawRawKeyRow(UserDataPrefsKeys.LevelCleared(3));
        DrawRawKeyRow(UserDataPrefsKeys.LevelThreeStar(3));

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Custom Bool PlayerPrefs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "임의 키에 bool을 원하는 저장 형식으로 쓰거나 삭제할 수 있습니다.",
            MessageType.None);

        _customKey = EditorGUILayout.TextField("Key", _customKey);
        _customValue = EditorGUILayout.Toggle("Value", _customValue);
        _customFormat = (UserDataBoolStorageFormat)EditorGUILayout.EnumPopup("Storage Format", _customFormat);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_customKey)))
            {
                if (GUILayout.Button("Write"))
                    WriteCustomKey();

                if (GUILayout.Button("Delete Key"))
                    DeleteCustomKey(_customKey);
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Tracked Custom Keys", EditorStyles.boldLabel);

        if (_trackedCustomKeys.Count == 0)
        {
            EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
        }
        else
        {
            for (int i = _trackedCustomKeys.Count - 1; i >= 0; i--)
            {
                string key = _trackedCustomKeys[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(key, UserDataStore.DescribeStoredValue(key));
                    if (GUILayout.Button("True", GUILayout.Width(44f)))
                        SetTrackedCustomKey(key, true);
                    if (GUILayout.Button("False", GUILayout.Width(44f)))
                        SetTrackedCustomKey(key, false);
                    if (GUILayout.Button("Del", GUILayout.Width(36f)))
                        DeleteCustomKey(key);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawLevelProgress(int level, LevelProgress progress)
    {
        EditorGUILayout.LabelField($"Level {level}", EditorStyles.miniBoldLabel);

        using (new EditorGUI.IndentLevelScope())
        {
            progress.IsFirstEntry = EditorGUILayout.Toggle("Is First Entry (seen)", progress.IsFirstEntry);
            progress.IsCleared = EditorGUILayout.Toggle("Cleared", progress.IsCleared);
            progress.IsThreeStarCleared = EditorGUILayout.Toggle("3-Star Cleared", progress.IsThreeStarCleared);
        }
    }

    private void DrawRawKeyRow(string key)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(key, GUILayout.Width(220f));
            EditorGUILayout.LabelField(UserDataStore.DescribeStoredValue(key));

            if (GUILayout.Button("0", GUILayout.Width(24f)))
            {
                UserDataStore.WriteBool(key, false, UserDataBoolStorageFormat.IntZeroOne);
                PlayerPrefs.Save();
                ReloadUserData();
            }

            if (GUILayout.Button("1", GUILayout.Width(24f)))
            {
                UserDataStore.WriteBool(key, true, UserDataBoolStorageFormat.IntZeroOne);
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

    private void WriteCustomKey()
    {
        string key = _customKey.Trim();
        if (string.IsNullOrEmpty(key))
            return;

        UserDataStore.WriteBool(key, _customValue, _customFormat);
        PlayerPrefs.Save();
        TrackCustomKey(key);
        ReloadUserData();
    }

    private void SetTrackedCustomKey(string key, bool value)
    {
        UserDataStore.WriteBool(key, value, _customFormat);
        PlayerPrefs.Save();
        ReloadUserData();
    }

    private void DeleteCustomKey(string key)
    {
        string trimmed = key?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        UserDataStore.DeleteKey(trimmed);
        PlayerPrefs.Save();
        _trackedCustomKeys.RemoveAll(k => k == trimmed);
        SaveTrackedCustomKeys();
        ReloadUserData();
    }

    private void TrackCustomKey(string key)
    {
        if (!_trackedCustomKeys.Contains(key))
        {
            _trackedCustomKeys.Add(key);
            SaveTrackedCustomKeys();
        }
    }

    private void LoadTrackedCustomKeys()
    {
        _trackedCustomKeys.Clear();

        string saved = EditorPrefs.GetString(CustomKeysEditorPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(saved))
            return;

        string[] keys = saved.Split('|', StringSplitOptions.RemoveEmptyEntries);
        _trackedCustomKeys.AddRange(keys);
    }

    private void SaveTrackedCustomKeys()
    {
        EditorPrefs.SetString(CustomKeysEditorPrefsKey, string.Join("|", _trackedCustomKeys));
    }
}
#endif
