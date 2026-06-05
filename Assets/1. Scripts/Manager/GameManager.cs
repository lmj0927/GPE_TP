using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// Game scenes only. Disables Python trainer connection before <see cref="Academy"/> initializes.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameManager : Singleton<GameManager>
{
    [SerializeField] private PlayerObstacleSpawner _obstacleSpawner;
    [SerializeField] private GameAgent _gameAgent;
    [SerializeField] private GameEndUI _gameEndUI;
    [SerializeField] private StoryUI _storyUI;
    [SerializeField] private int _level = 1;

    private float _runStartTime;
    private bool _runEnded;

    public float PlayTimeSeconds { get; private set; }
    public int SpawnCount { get; private set; }
    public int HitCount { get; private set; }

    protected override void Initialize()
    {
        base.Initialize();
        CommunicatorFactory.Enabled = false;
        Time.timeScale = 1f;
        _runEnded = false;
        _runStartTime = Time.time;
    }

    private void Start()
    {
        TryShowLevelStory();
    }

    private void TryShowLevelStory()
    {
        if (_level < 1 || _level > 3)
            return;

        var progress = UserDataStore.Load().GetLevel(_level);
        if (progress.IsFirstEntry)
            return;

        if (_storyUI == null)
            _storyUI = FindFirstObjectByType<StoryUI>();

        if (_storyUI == null)
            return;

        _storyUI.Show();
        UserDataStore.MarkLevelFirstEntryComplete(_level);
    }

    public void EndGame(bool isWin)
    {
        if (_runEnded)
            return;

        _runEnded = true;
        Time.timeScale = 1f;

        PlayTimeSeconds = Time.time - _runStartTime;

        var spawner = _obstacleSpawner != null
            ? _obstacleSpawner
            : FindFirstObjectByType<PlayerObstacleSpawner>();
        SpawnCount = spawner != null ? spawner.SpawnCount : 0;

        var agent = _gameAgent != null ? _gameAgent : FindFirstObjectByType<GameAgent>();
        HitCount = agent != null ? agent.HitCount : 0;

        if (_gameEndUI == null)
            _gameEndUI = FindFirstObjectByType<GameEndUI>();

        if (_gameEndUI == null)
        {
            Debug.LogWarning("[GameManager] GameEndUI not found.");
            return;
        }

        if (isWin)
        {
            var winData = new GameEndData
            {
                IsWin = true,
                PlayTimeSeconds = PlayTimeSeconds,
                SpawnCount = SpawnCount,
                HitCount = HitCount
            };

            _gameEndUI.Show(winData, stars => UserDataStore.RecordLevelWin(_level, stars));
        }
        else
        {
            _gameEndUI.Show(new GameEndData
            {
                IsWin = false,
                PlayTimeSeconds = PlayTimeSeconds,
                SpawnCount = SpawnCount,
                HitCount = HitCount
            });
        }
    }

#if UNITY_EDITOR
    public void PreviewEndThreeStarWin() => PreviewGameEnd(true, 3);

    public void PreviewEndTwoStarWin() => PreviewGameEnd(true, 2);

    public void PreviewEndOneStarWin() => PreviewGameEnd(true, 1);

    public void PreviewEndLose() => PreviewGameEnd(false, 0);

    private void PreviewGameEnd(bool isWin, int starCount)
    {
        if (_gameEndUI == null)
            _gameEndUI = FindFirstObjectByType<GameEndUI>();

        if (_gameEndUI == null)
        {
            Debug.LogWarning("[GameManager] GameEndUI not found.");
            return;
        }

        _gameEndUI.ShowPreview(isWin, starCount);

        if (isWin)
            UserDataStore.RecordLevelWin(_level, starCount);
    }
#endif
}

public class GameEndData
{
    public bool IsWin { get; set; }
    public float PlayTimeSeconds { get; set; }
    public int SpawnCount { get; set; }
    public int HitCount { get; set; }
}
