using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VisibilityZone : MonoBehaviour
{
    [Header("Base Vision")]
    [SerializeField] private float _baseViewRadius = 5f;
    [SerializeField] private SphereCollider _baseVisionCollider;

    [Header("Aiming Vision")]
    [SerializeField] private float _aimViewRadius = 8f;
    [SerializeField] private float _aimViewAngle = 60f;
    [SerializeField] private MeshCollider _aimVisionCollider;

    private Transform _playerTransform;
    private Mesh _aimVisionMesh;

    private void Awake()
    {
        _playerTransform = transform;
        
        // Настройка базового обзора
        _baseVisionCollider.radius = _baseViewRadius;
        _baseVisionCollider.isTrigger = true;
        
        // Настройка обзора при прицеливании
        _aimVisionMesh = CreateAimVisionMesh();
        _aimVisionCollider.sharedMesh = _aimVisionMesh;
        _aimVisionCollider.isTrigger = true;
    }

    private Mesh CreateAimVisionMesh()
    {
        Mesh mesh = new Mesh();
        int segments = 20;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        float angleStep = _aimViewAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -_aimViewAngle/2 + angleStep * i;
            Vector3 dir = Quaternion.Euler(0, currentAngle, 0) * _playerTransform.forward;
            vertices[i + 1] = dir * _aimViewRadius;

            if (i < segments)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        return mesh;
    }

    public void UpdateAimDirection(Vector3 aimDirection)
    {
        _aimVisionCollider.transform.rotation = Quaternion.LookRotation(aimDirection);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleVisibility(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<IVisible>(out var visibleObject))
        {
            Vector3 toObject = other.transform.position - _playerTransform.position;
            bool isInAimCone = Vector3.Angle(_aimVisionCollider.transform.forward, toObject) < _aimViewAngle/2;
            float checkDistance = isInAimCone ? _aimViewRadius : _baseViewRadius;

            bool hasObstacle = Physics.Linecast(
                _playerTransform.position,
                other.transform.position,
                LayerMask.GetMask("Obstacle"));

            visibleObject.SetVisible(!hasObstacle && toObject.magnitude <= checkDistance);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HandleVisibility(other, false);
    }

    private void HandleVisibility(Collider other, bool shouldBeVisible)
    {
        if (other.TryGetComponent<IVisible>(out var visibleObject))
        {
            visibleObject.SetVisible(shouldBeVisible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // // Базовый обзор
        // Gizmos.color = Color.cyan;
        // Gizmos.DrawWireSphere(transform.position, _baseViewRadius);
        
        // // Обзор при прицеливании
        // if (_aimVisionCollider != null)
        // {
        //     Gizmos.color = Color.magenta;
        //     Gizmos.DrawWireMesh(_aimVisionMesh, 
        //         _aimVisionCollider.transform.position, 
        //         _aimVisionCollider.transform.rotation);
        // }
    }
}