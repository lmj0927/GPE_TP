using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private MapGenerator _mapGenerator;

    private void Awake()
    {
        if (_mapGenerator == null)
            _mapGenerator = FindFirstObjectByType<MapGenerator>();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        var position = new Vector3(0f, target.position.y, target.position.z) + offset;

        if (_mapGenerator != null && _mapGenerator.HasEndTemplateWorldY)
            position.y = Mathf.Min(position.y, _mapGenerator.EndTemplateWorldY);

        transform.position = position;
    }
}
