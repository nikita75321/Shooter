using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class EnemiesInGame : MonoBehaviour
{
    public static EnemiesInGame Instance;

    [Header("Referencess")]
    [SerializeField] private SpawnPoints spawnPoints;

    [Header("Enemy")]
    [SerializeField] private Enemy enemyPrefab;
    // [SerializeField] private List<Enemy> enemies;
    [ShowInInspector] private Dictionary<string, Enemy> enemyModel = new();

    public Enemy EnemyPrefab => enemyPrefab;

    private void Awake()
    {
        Instance = this;
        if (enemyModel == null)
            enemyModel = new();
    }

    private void Start()
    {

    }

    private void Update()
    {

    }

    public void InitEnemies(PlayerInGameInfo playerInfo, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        Vector3 finalPosition = spawnPosition;
        Quaternion finalRotation = spawnRotation;

        bool hasServerPosition = finalPosition.sqrMagnitude > 0.0001f;

        if (!hasServerPosition && spawnPoints != null)
        {
            var spawnpoint = spawnPoints.GetRandomSpawnPoint();
            if (spawnpoint != null)
            {
                finalPosition = spawnpoint.position;
                finalRotation = spawnpoint.rotation;
            }
        }

        var enemy = Instantiate(enemyPrefab, finalPosition, finalRotation);

        enemy.transform.SetParent(transform);
        // enemies.Add(enemy);
        // Debug.Log($"playerId - {playerInfo.playerId}, enemy - {enemy}");
        enemyModel.TryAdd(playerInfo.playerId, enemy);

        playerInfo.position = finalPosition;
        playerInfo.rotation = finalRotation;

        enemy.InitHero(playerInfo);
    }

    public Enemy GetEnemy(string id)
    {
        // Debug.Log($"GetEnemy: id-{id}");
        if (enemyModel.TryGetValue(id, out var enemy))
            return enemy;
        else
            return null;
    }
}