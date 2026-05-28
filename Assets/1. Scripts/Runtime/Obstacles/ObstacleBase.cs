using System;
using UnityEngine;

/// <summary>
/// Hit trigger for climber; kinematic movement with optional CircleCast wall checks (no body collider required).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class ObstacleBase : MonoBehaviour, IPoolable
{
    [SerializeField] private ObstacleTuningConfig _tuning;
    [SerializeField] private Collider2D _hitTrigger;

    private Rigidbody2D _rigidbody;
    private bool _hitConsumed;
    private float _playerAimWorldX;
    private IClimberAgent _targetClimber;
    private Vector2? _spawnLaunchDirection;
    private Action<ObstacleBase> _releaseToPool;

    public abstract ObstacleKind Kind { get; }

    protected ObstacleTuningConfig Tuning => _tuning;
    protected Rigidbody2D Rigidbody => _rigidbody;
    protected float PlayerAimWorldX => _playerAimWorldX;
    protected IClimberAgent TargetClimber => _targetClimber;
    protected Vector2? SpawnLaunchDirection => _spawnLaunchDirection;

    public void BindPool(Action<ObstacleBase> releaseToPool) => _releaseToPool = releaseToPool;

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody.gravityScale = 0f;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (_hitTrigger != null)
            _hitTrigger.isTrigger = true;
    }

    public void OnSpawnedFromPool()
    {
    }

    public virtual void OnReturnedToPool()
    {
        _releaseToPool = null;
        _hitConsumed = false;
        _targetClimber = null;
        _spawnLaunchDirection = null;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector2.zero;
    }

    public void Activate(
        Vector2 worldPosition,
        float playerAimWorldX,
        IClimberAgent targetClimber = null,
        Vector2? launchDirection = null)
    {
        _playerAimWorldX = playerAimWorldX;
        _targetClimber = targetClimber;
        _spawnLaunchDirection = launchDirection;
        _hitConsumed = false;
        transform.position = worldPosition;
        gameObject.SetActive(true);
        OnActivated();
    }

    public void ReleaseToPool()
    {
        if (_releaseToPool != null)
            _releaseToPool(this);
        else
            DeactivateLocal();
    }

    internal void DeactivateLocal() => gameObject.SetActive(false);

    protected abstract void OnActivated();

    protected virtual void FixedUpdate()
    {
        OnFixedTick(Time.fixedDeltaTime);
        TryRecycleBelowTargetClimber();
    }

    protected abstract void OnFixedTick(float deltaTime);

    protected void TryRecycleBelowTargetClimber()
    {
        if (TargetClimber != null)
        {
            if (Tuning == null)
                return;

            float recycleY = TargetClimber.WorldPosition.y - Tuning.RecycleBelowTargetOffsetY;
            if (transform.position.y < recycleY)
                ReleaseToPool();
            return;
        }

        if (Tuning == null)
            return;

        if (transform.position.y < Tuning.RecycleBelowWorldY)
            ReleaseToPool();
    }

    protected void ApplyPosition(Vector2 position)
    {
        if (Rigidbody != null)
            Rigidbody.MovePosition(position);
        else
            transform.position = position;
    }

    protected void ApplyVelocity(Vector2 velocity)
    {
        if (Rigidbody != null)
            Rigidbody.linearVelocity = velocity;
    }

    protected void IgnoreEnemyCollisions(Collider2D physicsCollider)
    {
        if (physicsCollider == null || _tuning == null)
            return;

        var colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        for (int i = 0; i < colliders.Length; i++)
        {
            var col = colliders[i];
            if (col == null || col == physicsCollider || col == _hitTrigger)
                continue;

            if (!IsEnemyLayer(col.gameObject.layer))
                continue;

            Physics2D.IgnoreCollision(physicsCollider, col, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitConsumed || _hitTrigger == null || other == _hitTrigger)
            return;

        if (!other.isTrigger)
            TryHitClimber(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_hitConsumed || _hitTrigger == null)
            return;

        if (!other.isTrigger)
            TryHitClimber(other);
    }

    private void TryHitClimber(Collider2D other)
    {
        if (_tuning != null && !IsEnemyLayer(other.gameObject.layer))
            return;

        if (!other.TryGetComponent(out IClimberAgent climber))
            return;
        

        climber.ApplyHit();
        _hitConsumed = true;
        OnClimberHit();
    }

    protected virtual void OnClimberHit()
    {
        ReleaseToPool();
    }

    private bool IsEnemyLayer(int layer)
    {
        if (_tuning == null)
            return false;

        return (_tuning.EnemyLayers.value & (1 << layer)) != 0;
    }
}
