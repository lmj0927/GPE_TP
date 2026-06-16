using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelProgress
{
    /// <summary>false = 해당 레벨 최초 진입(스토리 미표시), true = 이미 진입한 적 있음.</summary>
    public bool IsFirstEntry;

    /// <summary>0 = 미클리어, 1–3 = 최고 달성 별 개수.</summary>
    public int Star;

    public bool IsCleared => Star > 0;
}

[Serializable]
public class UserData
{
    public LevelProgress Level1 = new();
    public LevelProgress Level2 = new();
    public LevelProgress Level3 = new();
    public int Upgrade1;
    public int Upgrade2;
    public int Upgrade3;
    public int currency;

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
    public static string LevelStar(int level) => $"{Prefix}Level{level}_Star";
    public static string Currency => $"{Prefix}Currency";
    public static string Upgrade1 => $"{Prefix}Upgrade1";
    public static string Upgrade2 => $"{Prefix}Upgrade2";
    public static string Upgrade3 => $"{Prefix}Upgrade3";

    public static IEnumerable<string> GetAllManagedKeys()
    {
        for (int level = 1; level <= 3; level++)
        {
            yield return LevelFirstEntry(level);
            yield return LevelStar(level);
        }

        yield return Currency;
        yield return Upgrade1;
        yield return Upgrade2;
        yield return Upgrade3;
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
                Star = ReadStar(UserDataPrefsKeys.LevelStar(level))
            });
        }

        data.currency = ReadCurrency(UserDataPrefsKeys.Currency);
        data.Upgrade1 = ReadUpgrade(UserDataPrefsKeys.Upgrade1);
        data.Upgrade2 = ReadUpgrade(UserDataPrefsKeys.Upgrade2);
        data.Upgrade3 = ReadUpgrade(UserDataPrefsKeys.Upgrade3);
        return data;
    }

    public static void Save(UserData data)
    {
        for (int level = 1; level <= 3; level++)
        {
            var progress = data.GetLevel(level);
            WriteBool(UserDataPrefsKeys.LevelFirstEntry(level), progress.IsFirstEntry);
            WriteStar(UserDataPrefsKeys.LevelStar(level), progress.Star);
        }

        WriteCurrency(UserDataPrefsKeys.Currency, data.currency);
        WriteUpgrade(UserDataPrefsKeys.Upgrade1, data.Upgrade1);
        WriteUpgrade(UserDataPrefsKeys.Upgrade2, data.Upgrade2);
        WriteUpgrade(UserDataPrefsKeys.Upgrade3, data.Upgrade3);
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
        progress.Star = Mathf.Max(progress.Star, Mathf.Clamp(starCount, 0, 3));

        data.SetLevel(level, progress);
        Save(data);
    }

    public static void AddCurrency(int amount)
    {
        if (amount <= 0)
            return;

        var data = Load();
        data.currency = Mathf.Max(0, data.currency + amount);
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

    private static int ReadCurrency(string key) =>
        Mathf.Max(0, PlayerPrefs.GetInt(key, 0));

    private static void WriteCurrency(string key, int amount) =>
        PlayerPrefs.SetInt(key, Mathf.Max(0, amount));

    private static int ReadUpgrade(string key) =>
        Mathf.Clamp(PlayerPrefs.GetInt(key, 0), 0, 10);

    private static void WriteUpgrade(string key, int amount) =>
        PlayerPrefs.SetInt(key, Mathf.Clamp(amount, 0, 10));
}
