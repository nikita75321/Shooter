using System.Collections.Generic;
using UnityEngine;

public class BotsInGame : MonoBehaviour
{
    public static BotsInGame Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SpawnPoints spawnPoints;
    [SerializeField] private Enemy botPrefab;

    private Dictionary<string, Enemy> activeBots = new Dictionary<string, Enemy>();

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

    public void SpawnBots(IList<PlayerInGameInfo> botInfos)
    {
        ClearBots();

        if (botInfos == null || botInfos.Count == 0)
        {
            return;
        }

        foreach (var botInfo in botInfos)
        {
            SpawnSingleBot(botInfo);
        }
    }

    private void SpawnSingleBot(PlayerInGameInfo botInfo)
    {
        if (botInfo == null)
        {
            return;
        }

        PlayerInGameInfo runtimeInfo = BuildRuntimeBotInfo(botInfo);
        if (runtimeInfo == null || string.IsNullOrEmpty(runtimeInfo.playerId))
        {
            return;
        }

        if (activeBots.ContainsKey(runtimeInfo.playerId))
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
        botInstance.name = $"Bot_{runtimeInfo.playerId}";
        botInstance.InitHero(runtimeInfo);
        ApplyServerStats(botInstance, runtimeInfo);

        activeBots.Add(runtimeInfo.playerId, botInstance);
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

    private PlayerInGameInfo BuildRuntimeBotInfo(PlayerInGameInfo source)
    {
        if (source == null)
        {
            return null;
        }

        int heroCount = Level.Instance != null && Level.Instance.heroDatas != null
            ? Level.Instance.heroDatas.Length
            : 0;

        int sanitizedHeroId = source.hero_id;
        if (heroCount > 0)
        {
            sanitizedHeroId = Mathf.Clamp(sanitizedHeroId, 0, heroCount - 1);
        }
        else
        {
            sanitizedHeroId = Mathf.Max(0, sanitizedHeroId);
        }

        string botId = string.IsNullOrEmpty(source.playerId)
            ? (string.IsNullOrEmpty(source.player_name) ? null : source.player_name)
            : source.playerId;

        if (string.IsNullOrEmpty(botId))
        {
            return null;
        }

        string botName = string.IsNullOrEmpty(source.player_name) ? botId : source.player_name;

        var runtimeInfo = new PlayerInGameInfo(botId, botName, source.rating, sanitizedHeroId)
        {
            hero_skin = Mathf.Max(0, source.hero_skin),
            hero_level = Mathf.Max(0, source.hero_level),
            hero_rank = Mathf.Max(0, source.hero_rank),
            hp = source.hp > 0 ? source.hp : Mathf.Max(source.max_hp, 0f),
            armor = Mathf.Max(0f, source.armor),
            max_hp = Mathf.Max(source.max_hp, 0f),
            max_armor = Mathf.Max(source.max_armor, 0f),
            isAlive = source.isAlive,
            kills = source.kills,
            deaths = source.deaths,
            animationState = source.animationState,
            boolsState = source.boolsState,
            current_weapon = source.current_weapon,
            isReady = source.isReady,
            noizeRadius = source.noizeRadius,
            position = source.position,
            rotation = source.rotation
        };

        return runtimeInfo;
    }

    private void ApplyServerStats(Enemy botInstance, PlayerInGameInfo botInfo)
    {
        if (botInstance == null || botInfo == null)
        {
            return;
        }

        if (botInfo.max_hp > 0f)
        {
            float maxHp = Mathf.Max(botInfo.max_hp, 0f);
            botInstance.Health.MaxHealth = maxHp;
            float clampedHp = Mathf.Clamp(botInfo.hp, 0f, maxHp);
            botInstance.Health.ChangeHp(clampedHp);
        }

        float maxArmor = Mathf.Max(botInfo.max_armor, 0f);
        botInstance.Armor.MaxArmor = maxArmor;
        float clampedArmor = maxArmor > 0f ? Mathf.Clamp(botInfo.armor, 0f, maxArmor) : Mathf.Max(botInfo.armor, 0f);
        botInstance.Armor.ChangeArmor(clampedArmor);
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