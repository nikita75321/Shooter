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

    public void InitEnemies(int idHero, string playerId, int idSkin = 0)
    {
        var spawnpoint = spawnPoints.GetRandomSpawnPoint();
        var enemy = Instantiate(enemyPrefab, spawnpoint.position, Quaternion.identity);

        enemy.transform.SetParent(transform);
        // enemies.Add(enemy);
        Debug.Log($"playerId - {playerId}, enemy - {enemy}");
        enemyModel.TryAdd(playerId, enemy);

        enemy.InitHero(idHero, idSkin);
    }

    public Enemy GetEnemy(string id)
    {
        if (enemyModel.TryGetValue(id, out var enemy))
            return enemy;
        else
            return null;
    }
}