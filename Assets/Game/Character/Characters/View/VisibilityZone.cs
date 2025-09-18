using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VisibilityZone : MonoBehaviour
{
    [Header("Base Vision")]
    public float BaseViewRadius
    {
        get => _baseViewRadius;
        set
        {
            _baseViewRadius = Mathf.Max(0f, value);
            if (_baseVisionCollider) _baseVisionCollider.radius = _baseViewRadius;
        }
    }
    [SerializeField] private float _baseViewRadius = 5f;
    [SerializeField] private SphereCollider _baseVisionCollider;

    [Header("Aiming Vision")]
    public float AimViewRadius
    {
        get => _aimViewRadius;
        set
        {
            _aimViewRadius = Mathf.Max(0f, value);
            RebuildAimVision();
        }
    }
    [SerializeField] private float _aimViewRadius = 8f;
    [SerializeField, Range(1f, 179f)] private float _aimViewAngle = 60f;
    [SerializeField] private MeshCollider _aimVisionCollider;

    private Transform _playerTransform;
    private Mesh _aimVisionMesh;

    private const int SEGMENTS = 20;

    private void RebuildAimVision()
    {
        if (_aimVisionMesh == null) _aimVisionMesh = new Mesh { name = "AimVisionMesh" };

        // Твоя генерация меша в ЛОКАЛЬНОМ пространстве (как в прошлом сообщении)
        CreateAimVisionMesh(_aimVisionMesh, 20, _aimViewAngle, _aimViewRadius);

        if (_aimVisionCollider)
        {
            // ВАЖНО: чтобы MeshCollider применил новые вершины, переназначаем sharedMesh
            _aimVisionCollider.sharedMesh = null;
            _aimVisionCollider.sharedMesh = _aimVisionMesh;
            _aimVisionCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        _playerTransform = transform;

        if (_baseVisionCollider)
        {
            _baseVisionCollider.isTrigger = true;
            _baseVisionCollider.radius = _baseViewRadius; // начальная инициализация
        }

        if (_aimVisionCollider) _aimVisionCollider.isTrigger = true;
        RebuildAimVision(); // построить начальный меш
    }

    // Обновлять меш при изменении параметров в инспекторе (в редакторе)
    private void OnValidate()
    {
        if (_baseVisionCollider) _baseVisionCollider.radius = _baseViewRadius;
        RebuildAimVision();
    }

    /// <summary>
    /// Генерируем сектор в ЛОКАЛЬНОМ пространстве (плоскость XZ, направление +Z).
    /// </summary>
    private static void CreateAimVisionMesh(Mesh mesh, int segments, float angleDeg, float radius)
    {
        var vertices = new Vector3[segments + 2];
        var triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        float angleStep = angleDeg / segments;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -angleDeg * 0.5f + angleStep * i;
            // Локальное направление (не зависит от transform.forward):
            Vector3 dir = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.forward;
            vertices[i + 1] = dir * radius;

            if (i < segments)
            {
                int tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = i + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void UpdateAimDirection(Vector3 aimDirection)
    {
        if (_aimVisionCollider != null)
            _aimVisionCollider.transform.rotation = Quaternion.LookRotation(aimDirection);
    }

    private void OnTriggerEnter(Collider other) => HandleVisibility(other, true);

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<IVisible>(out var visibleObject))
        {
            Vector3 toObject = other.transform.position - _playerTransform.position;
            bool isInAimCone = _aimVisionCollider != null &&
                               Vector3.Angle(_aimVisionCollider.transform.forward, toObject) < _aimViewAngle * 0.5f;

            float checkDistance = isInAimCone ? _aimViewRadius : _baseViewRadius;

            bool hasObstacle = Physics.Linecast(
                _playerTransform.position,
                other.transform.position,
                LayerMask.GetMask("Obstacle"));

            visibleObject.SetVisible(!hasObstacle && toObject.magnitude <= checkDistance);
        }
    }

    private void OnTriggerExit(Collider other) => HandleVisibility(other, false);

    private void HandleVisibility(Collider other, bool shouldBeVisible)
    {
        if (other.TryGetComponent<IVisible>(out var visibleObject))
            visibleObject.SetVisible(shouldBeVisible);
    }

    private void OnDrawGizmosSelected()
    {
        // Базовый обзор
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _baseViewRadius);

        // Обзор при прицеливании (кастомный меш)
        if (_aimVisionMesh == null)
        {
            _aimVisionMesh = new Mesh { name = "AimVisionMesh (Gizmos)" };
            CreateAimVisionMesh(_aimVisionMesh, SEGMENTS, _aimViewAngle, _aimViewRadius);
        }

        // Позиция/поворот берём из коллайдера, если он задан, иначе — из самого объекта
        Vector3 pos = _aimVisionCollider != null ? _aimVisionCollider.transform.position : transform.position;
        Quaternion rot = _aimVisionCollider != null ? _aimVisionCollider.transform.rotation : transform.rotation;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireMesh(_aimVisionMesh, pos, rot, Vector3.one);
    }
}