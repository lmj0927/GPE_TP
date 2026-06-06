using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelProgress
{
    /// <summary>false = 해당 레벨 최초 진입(스토리 미표시), true = 이미 진입한 적 있음.</summary>
    public bool IsFirstEntry;
    public bool IsCleared;

    /// <summary>0 = 미클리어, 1–3 = 최고 달성 별 개수.</summary>
    public int Star;
}

[Serializable]
public class UserData
{
    public LevelProgress Level1 = new();
    public LevelProgress Level2 = new();
    public LevelProgress Level3 = new();

    public LevelProgress GetLevel(int level)
    {
        return level switch
        {
            1 => Level1,
            2 => Level2,
            3 => Level3,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Level must be 1, 2, or 3.")
        };
    }

    public void SetLevel(int level, LevelProgress progress)
    {
        switch (level)
        {
            case 1: Level1 = progress; break;
            case 2: Level2 = progress; break;
            case 3: Level3 = progress; break;
            default: throw new ArgumentOutOfRangeException(nameof(level), level, "Level must be 1, 2, or 3.");
        }
    }
}

public static class UserDataPrefsKeys
{
    public const string Prefix = "UserData_";

    public static string LevelFirstEntry(int level) => $"{Prefix}Level{level}_FirstEntry";
    public static string LevelCleared(int level) => $"{Prefix}Level{level}_Cleared";
    public static string LevelStar(int level) => $"{Prefix}Level{level}_Star";

    public static IEnumerable<string> GetAllManagedKeys()
    {
        for (int level = 1; level <= 3; level++)
        {
            yield return LevelFirstEntry(level);
            yield return LevelCleared(level);
            yield return LevelStar(level);
        }
    }
}

public static class UserDataStore
{
    public static UserData Load()
    {
        var data = new UserData();

        for (int level = 1; level <= 3; level++)
        {
            data.SetLevel(level, new LevelProgress
            {
                IsFirstEntry = ReadBool(UserDataPrefsKeys.LevelFirstEntry(level)),
                IsCleared = ReadBool(UserDataPrefsKeys.LevelCleared(level)),
                Star = ReadStar(UserDataPrefsKeys.LevelStar(level))
            });
        }

        return data;
    }

    public static void Save(UserData data)
    {
        for (int level = 1; level <= 3; level++)
        {
            var progress = data.GetLevel(level);
            WriteBool(UserDataPrefsKeys.LevelFirstEntry(level), progress.IsFirstEntry);
            WriteBool(UserDataPrefsKeys.LevelCleared(level), progress.IsCleared);
            WriteStar(UserDataPrefsKeys.LevelStar(level), progress.Star);
        }

        PlayerPrefs.Save();
    }

    public static void RecordLevelWin(int level, int starCount)
    {
        if (level < 1 || level > 3)
        {
            Debug.LogWarning($"[UserDataStore] Invalid level {level}. Progress was not saved.");
            return;
        }

        var data = Load();
        var progress = data.GetLevel(level);
        progress.IsCleared = true;
        progress.Star = Mathf.Max(progress.Star, Mathf.Clamp(starCount, 0, 3));

        data.SetLevel(level, progress);
        Save(data);
    }

    public static void MarkLevelFirstEntryComplete(int level)
    {
        if (level < 1 || level > 3)
        {
            Debug.LogWarning($"[UserDataStore] Invalid level {level}. First-entry flag was not saved.");
            return;
        }

        var data = Load();
        var progress = data.GetLevel(level);
        if (progress.IsFirstEntry)
            return;

        progress.IsFirstEntry = true;
        data.SetLevel(level, progress);
        Save(data);
    }

    public static void DeleteAllUserDataKeys()
    {
        foreach (string key in UserDataPrefsKeys.GetAllManagedKeys())
            DeleteKey(key);

        PlayerPrefs.Save();
    }

    public static void DeleteKey(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return;

        PlayerPrefs.DeleteKey(key);
    }

    private static bool ReadBool(string key) => PlayerPrefs.GetInt(key, 0) != 0;

    private static void WriteBool(string key, bool value) =>
        PlayerPrefs.SetInt(key, value ? 1 : 0);

    private static int ReadStar(string key) =>
        Mathf.Clamp(PlayerPrefs.GetInt(key, 0), 0, 3);

    private static void WriteStar(string key, int star) =>
        PlayerPrefs.SetInt(key, Mathf.Clamp(star, 0, 3));
}
