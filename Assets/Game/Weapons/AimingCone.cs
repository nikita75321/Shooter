using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class AimingCone : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Player player;
    [SerializeField] private Weapon weapon;
    [SerializeField] private CharacterController character;
    
    [Header("Visual Settings")]
    [SerializeField] private Material coneMaterial;
    // [SerializeField] private Color neutralColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private Color neutralColor = Color.gray;
    [SerializeField] private Color detectedColor = new Color(1, 0, 0, 0.5f);

    [Header("Detection Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int baseRays = 5;
    [SerializeField] private float raysPerDegree = 0.5f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private bool hasDetection;

    [Header("Enemies")]
    [SerializeField] private List<Transform> detectedEnemies = new List<Transform>();

    private void OnValidate()
    {

    }

    private void Awake()
    {
        character = GetComponentInParent<CharacterController>();

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = new Material(coneMaterial);
    }

    private void Update()
    {
        // if (GameStateManager.Instance.GameState == GameState.game)
        // {
        //     UpdateVisuals();
        //     if (!player.IsUseAidKit)
        //         ScanForEnemies();
        // }
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            UpdateVisuals();
            if (!player.IsUseAidKit && !player.IsRevive)
            {
                // Debug.Log(1);
                ScanForEnemies();
                // Обновляем направление зоны видимости
                player.visibilityZone.UpdateAimDirection(transform.forward);
            }
            else
            {
                HideVisual();
                // Debug.Log(2);
            }
        }
    }

    public void Init(Weapon newWeapon)
    {
        weapon = newWeapon;
    }

    private void ScanForEnemies()
    {
        detectedEnemies.Clear();
        float angle = weapon.currentAimAngle;
        float distance = weapon.range;
        int rayCount = baseRays + Mathf.RoundToInt(angle * raysPerDegree);
        float angleStep = angle * 2 / rayCount;

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = -angle + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * transform.forward;

            RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, distance, enemyLayer | obstacleLayer);

            foreach (RaycastHit hit in hits)
            {
                // Если это препятствие - прерываем проверку по этому лучу
                if (((1 << hit.collider.gameObject.layer) & obstacleLayer) != 0)
                    break;

                // Если это враг и еще не добавлен
                if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
                {
                    if (!detectedEnemies.Contains(hit.transform))
                    {
                        detectedEnemies.Add(hit.transform);
                        weapon.TryShoot();
                    }
                }
            }
        }

        hasDetection = detectedEnemies.Count > 0;

        if (hasDetection)
        {
            player.IsShoot = true;
            if (weapon.currentAmmo == 0)
            {
                weapon.StartReload();
            }
            player.Controller.animator.SetBool("IsShoot", true);

            if (player.Character.CurrentWeapon.animator != null)
            {
                player.Character.CurrentWeapon.animator.SetBool("IsShoot", true);
            }
        }
        else
        {
            player.IsShoot = false;
            player.Controller.animator.SetBool("IsShoot", false);
            if (player.Character.CurrentWeapon.animator != null)
            {
                player.Character.CurrentWeapon.animator.SetBool("IsShoot", false);
            }
        }
    }

    private void UpdateVisuals()
    {
        if (!player.IsReload && !player.IsUseAidKit)
        {
            meshRenderer.material.color = hasDetection ? detectedColor : neutralColor;
            DrawConeMesh();
        }
        else
        {
            HideVisual();
        }
    }

    private void HideVisual()
    {
        meshFilter.mesh = null;
    }

    private void DrawConeMesh()
    {
        // Debug.Log("Draw aim mesh");
        Mesh mesh = new();
        float angle = weapon.currentAimAngle;
        float distance = weapon.range;
        int segments = 20;
        // Debug.Log(angle);

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        float angleStep = angle * 2 / segments;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -angle + angleStep * i;
            Vector3 dir = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward;
            vertices[i + 1] = dir * distance;

            if (i < segments)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshFilter.mesh = mesh;
    }

    private void OnDrawGizmos()
    {
        if (!weapon) return;

        float angle = weapon.currentAimAngle;
        float distance = weapon.range;
        int rayCount = baseRays + Mathf.RoundToInt(angle * raysPerDegree);
        float angleStep = angle * 2 / rayCount;

        Gizmos.color = hasDetection ? Color.red : Color.green;
        
        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = -angle + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, direction * distance);
        }
    }
}