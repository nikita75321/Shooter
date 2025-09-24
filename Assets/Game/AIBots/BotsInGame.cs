using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BotsInGame : MonoBehaviour
{
    public static BotsInGame Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SpawnPoints spawnPoints;
    [SerializeField] private Enemy botPrefab;

    [ShowInInspector] private Dictionary<string, Enemy> activeBots = new Dictionary<string, Enemy>();

    private void Awake()
    {
        Instance = this;

        if (activeBots == null)
        {
            activeBots = new Dictionary<string, Enemy>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        if (spawnPoints == null)
        {
            spawnPoints = GetComponentInParent<SpawnPoints>();

            if (spawnPoints == null)
            {
                var level = GetComponentInParent<LevelPrefab>();
                if (level != null)
                {
                    spawnPoints = level.spawnPoints;
                }
            }
        }
    }

    public void SpawnBots(IList<string> botIds)
    {
        ClearBots();

        if (botIds == null || botIds.Count == 0)
        {
            return;
        }

        foreach (var botId in botIds)
        {
            SpawnSingleBot(botId);
        }
    }

    private void SpawnSingleBot(string botId)
    {
        if (string.IsNullOrEmpty(botId) || activeBots.ContainsKey(botId))
        {
            return;
        }

        Enemy prefab = GetBotPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("BotsInGame: Bot prefab is not assigned and enemy prefab is not available.");
            return;
        }

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = Quaternion.identity;

        if (spawnPoints != null)
        {
            var spawnPoint = spawnPoints.GetRandomSpawnPoint();
            if (spawnPoint != null)
            {
                spawnPosition = spawnPoint.position;
                spawnRotation = spawnPoint.rotation;
            }
        }

        Enemy botInstance = Instantiate(prefab, spawnPosition, spawnRotation, transform);
        botInstance.name = $"Bot_{botId}";

        PlayerInGameInfo botInfo = CreateBotInfo(botId);
        botInstance.InitHero(botInfo);

        activeBots.Add(botId, botInstance);
    }

    private Enemy GetBotPrefab()
    {
        if (botPrefab != null)
        {
            return botPrefab;
        }

        if (EnemiesInGame.Instance != null)
        {
            return EnemiesInGame.Instance.EnemyPrefab;
        }

        return null;
    }

    private PlayerInGameInfo CreateBotInfo(string botId)
    {
        int heroIndex = 0;
        float maxHp = 100f;
        float maxArmor = 0f;

        if (Level.Instance != null && Level.Instance.heroDatas != null && Level.Instance.heroDatas.Length > 0)
        {
            heroIndex = Mathf.Clamp(UnityEngine.Random.Range(0, Level.Instance.heroDatas.Length), 0, Level.Instance.heroDatas.Length - 1);
            var heroData = Level.Instance.heroDatas[heroIndex];
            maxHp = heroData.health;
            maxArmor = heroData.armor;
        }

        var info = new PlayerInGameInfo(botId, botId, 0, heroIndex)
        {
            hero_skin = 0,
            hero_level = 0,
            hero_rank = 0,
            max_hp = maxHp,
            hp = maxHp,
            max_armor = maxArmor,
            armor = maxArmor,
            isAlive = true
        };

        return info;
    }

    public void ClearBots()
    {
        if (activeBots == null || activeBots.Count == 0)
        {
            return;
        }

        foreach (var bot in activeBots.Values)
        {
            if (bot != null)
            {
                Destroy(bot.gameObject);
            }
        }

        activeBots.Clear();
    }
}