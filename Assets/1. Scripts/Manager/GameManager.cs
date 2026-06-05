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

    private float _runStartTime;

    public float PlayTimeSeconds { get; private set; }
    public int SpawnCount { get; private set; }
    public int HitCount { get; private set; }

    protected override void Initialize()
    {
        base.Initialize();
        CommunicatorFactory.Enabled = false;
        _runStartTime = Time.time;
    }

    public void EndGame(bool isWin)
    {
        PlayTimeSeconds = Time.time - _runStartTime;

        var spawner = _obstacleSpawner != null
            ? _obstacleSpawner
            : FindFirstObjectByType<PlayerObstacleSpawner>();
        SpawnCount = spawner != null ? spawner.SpawnCount : 0;

        var agent = _gameAgent != null ? _gameAgent : FindFirstObjectByType<GameAgent>();
        HitCount = agent != null ? agent.HitCount : 0;

        if (isWin)
        {
            _gameEndUI.Show(new GameEndData
            {
                IsWin = true,
                PlayTimeSeconds = PlayTimeSeconds,
                SpawnCount = SpawnCount,
                HitCount = HitCount
            });
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
