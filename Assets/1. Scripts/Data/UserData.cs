using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelProgress
{
    /// <summary>false = 해당 레벨 최초 진입(스토리 미표시), true = 이미 진입한 적 있음.</summary>
    public bool IsFirstEntry;
    public bool IsCleared;
    public bool IsThreeStarCleared;
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
    public static string LevelThreeStar(int level) => $"{Prefix}Level{level}_ThreeStar";

    public static IEnumerable<string> GetAllManagedKeys()
    {
        for (int level = 1; level <= 3; level++)
        {
            yield return LevelFirstEntry(level);
            yield return LevelCleared(level);
            yield return LevelThreeStar(level);
        }
    }
}

public enum UserDataBoolStorageFormat
{
    IntZeroOne,
    IntOneZero,
    StringTrueFalse
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
                IsFirstEntry = ReadBool(UserDataPrefsKeys.LevelFirstEntry(level), UserDataBoolStorageFormat.IntZeroOne),
                IsCleared = ReadBool(UserDataPrefsKeys.LevelCleared(level), UserDataBoolStorageFormat.IntZeroOne),
                IsThreeStarCleared = ReadBool(UserDataPrefsKeys.LevelThreeStar(level), UserDataBoolStorageFormat.IntZeroOne)
            });
        }

        return data;
    }

    public static void Save(UserData data)
    {
        for (int level = 1; level <= 3; level++)
        {
            var progress = data.GetLevel(level);
            WriteBool(UserDataPrefsKeys.LevelFirstEntry(level), progress.IsFirstEntry, UserDataBoolStorageFormat.IntZeroOne);
            WriteBool(UserDataPrefsKeys.LevelCleared(level), progress.IsCleared, UserDataBoolStorageFormat.IntZeroOne);
            WriteBool(UserDataPrefsKeys.LevelThreeStar(level), progress.IsThreeStarCleared, UserDataBoolStorageFormat.IntZeroOne);
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

        if (starCount >= 3)
            progress.IsThreeStarCleared = true;

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

    public static bool HasKey(string key) => PlayerPrefs.HasKey(key);

    public static void DeleteKey(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return;

        PlayerPrefs.DeleteKey(key);
    }

    public static bool ReadBool(string key, UserDataBoolStorageFormat format)
    {
        if (!PlayerPrefs.HasKey(key))
            return false;

        return format switch
        {
            UserDataBoolStorageFormat.IntZeroOne => PlayerPrefs.GetInt(key, 0) != 0,
            UserDataBoolStorageFormat.IntOneZero => PlayerPrefs.GetInt(key, 1) == 0,
            UserDataBoolStorageFormat.StringTrueFalse => PlayerPrefs.GetString(key, "false")
                .Equals("true", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public static void WriteBool(string key, bool value, UserDataBoolStorageFormat format)
    {
        switch (format)
        {
            case UserDataBoolStorageFormat.IntZeroOne:
                PlayerPrefs.SetInt(key, value ? 1 : 0);
                break;
            case UserDataBoolStorageFormat.IntOneZero:
                PlayerPrefs.SetInt(key, value ? 0 : 1);
                break;
            case UserDataBoolStorageFormat.StringTrueFalse:
                PlayerPrefs.SetString(key, value ? "true" : "false");
                break;
        }
    }

    public static string DescribeStoredValue(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return "(missing)";

        string stringValue = PlayerPrefs.GetString(key, string.Empty);
        if (stringValue.Equals("true", StringComparison.OrdinalIgnoreCase)
            || stringValue.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return $"string=\"{stringValue}\"";
        }

        return $"int={PlayerPrefs.GetInt(key)}";
    }
}
