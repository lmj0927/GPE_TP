using UnityEngine;

/// <summary>
/// Syncs <see cref="PlayerObstacleInput"/> spawn mode to <see cref="SkillCooldownUI"/> slot selection visuals.
/// </summary>
public sealed class SkillBarController : MonoBehaviour
{
    [SerializeField] private PlayerObstacleInput _obstacleInput;

    private SkillCooldownUI[] _slots;

    private void Awake()
    {
        _slots = GetComponentsInChildren<SkillCooldownUI>(true);

        if (_obstacleInput == null)
            _obstacleInput = FindFirstObjectByType<PlayerObstacleInput>();
    }

    private void OnEnable()
    {
        if (_obstacleInput == null)
            return;

        _obstacleInput.ObstacleKindChanged += ApplySelection;
    }

    private void Start()
    {
        if (_obstacleInput != null)
            ApplySelection(_obstacleInput.CurrentObstacleKind);
    }

    private void OnDisable()
    {
        if (_obstacleInput == null)
            return;

        _obstacleInput.ObstacleKindChanged -= ApplySelection;
    }

    private void ApplySelection(ObstacleKind selectedKind)
    {
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (slot == null)
                continue;

            slot.SetSelected(slot.obstacleKind == selectedKind);
        }
    }
}
