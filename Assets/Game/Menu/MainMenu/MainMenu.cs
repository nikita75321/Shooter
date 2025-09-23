using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

public static class DataExtensions
{
    public static int ToInt(this object value, int defaultValue = 0)
    {
        if (value == null) return defaultValue;
        return int.TryParse(value.ToString(), out int result) ? result : defaultValue;
    }

    public static bool ToBool(this object value, bool defaultValue = false)
    {
        if (value == null) return defaultValue;
        return bool.TryParse(value.ToString(), out bool result) ? result : defaultValue;
    }

    public static object GetValueOrDefault(this Dictionary<string, object> dict, string key, object defaultValue)
    {
        return dict.TryGetValue(key, out object value) ? value : defaultValue;
    }
}

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance;

    [Header("Referencess")]
    [SerializeField] private SettingsMenu settings;

    [Header("Camera")]
    [SerializeField] private Camera menuCamera;

    [Header("Main menu")]
    [SerializeField] private GameObject[] mainMenuGO;
    [SerializeField] private TMP_Text favoriteHeroText;

    [Header("Start game")]
    [SerializeField] private GameObject loadCanvas;

    [Header("Object To Init")]
    [SerializeField] private GameObject[] objectsToInit;

    private void Awake()
    {
        Instance = this;
    }

    private void InitObject()
    {
        foreach (var obj in objectsToInit)
        {
            obj.SetActive(true);
            obj.SetActive(false);
        }
    }

    private void Start()
    {
        InitObject();
        WebSocketBase.Instance.OnServerDataReceived += LoadServerData;
        // if (Geekplay.Instance.PlayerData != null)
        // {
        //     var id = Geekplay.Instance.PlayerData.id;

        //     DOVirtual.DelayedCall(5f, ()=>
        //     {
        //         WebSocketBase.Instance.RequestPlayerData(id);	
        //         Debug.Log("Load server data");
        //     });
        // }
        // else
        // {
        //     Debug.Log("Geekplay.Instance == null");
        // }
    }

    private void OnDestroy()
    {
        WebSocketBase.Instance.OnServerDataReceived -= LoadServerData;
    }

    public void OpenMenu()
    {
        foreach (var go in mainMenuGO)
        {
            go.SetActive(true);
        }
        menuCamera.gameObject.SetActive(true);
    }

    public void CloseMenu()
    {
        foreach (var go in mainMenuGO)
        {
            go.SetActive(false);
        }
        menuCamera.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        int heroId = Geekplay.Instance.PlayerData.currentHero;
        Geekplay.Instance.PlayerData.heroMatch[heroId]++;
        Geekplay.Instance.Save();

        // Сохранение в БД
        SaveHeroStatsToDatabase(heroId);

        CloseMenu();
        // WebSocketBase.Instance.JoinMatchmaking(Geekplay.Instance.PlayerData.currentMode);
        loadCanvas.SetActive(true);
    }

    // Вспомогательный метод для получения имени героя по ID
    public string GetHeroNameById(int heroId)
    {
        // Здесь должна быть ваша логика получения имени героя
        // Например, можно использовать массив или перечисление
        string[] heroNames = {
            "Kayel", "Coco", "Bobby", "Mono",
            "Freddy", "Ci-J", "Zetta", "Rambo"
        };

        return heroId >= 0 && heroId < heroNames.Length
            ? heroNames[heroId]
            : "Unknown";
    }

    // Метод для открытия нового героя
    public void UnlockHero(int heroId)
    {
        if (heroId >= 0 && heroId < Geekplay.Instance.PlayerData.openHeroes.Length)
        {
            Geekplay.Instance.PlayerData.openHeroes[heroId] = 1;
            Geekplay.Instance.Save();
        }
    }

    public void SaveHeroStatsToDatabase(int heroId)
    {
        var playerData = Geekplay.Instance.PlayerData;
        WebSocketBase.Instance.UpdateHeroStats(
            heroId,
            playerData.heroMatch[heroId],
            heroId == playerData.favoriteHero
        );
    }

    private void LoadServerData(Dictionary<string, object> serverData)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log("Load server data player");
            if (serverData == null || Geekplay.Instance?.PlayerData == null)
            {
                Debug.LogError("Failed to load server data - null reference");
                return;
            }

            try
            {
                // 1. Основная информация игрока
                Geekplay.Instance.PlayerData.id = serverData.GetValueOrDefault("id", Geekplay.Instance.PlayerData.id).ToString();
                Geekplay.Instance.PlayerData.name = serverData.GetValueOrDefault("player_name", Geekplay.Instance.PlayerData.name).ToString();

                // 2. Статистика и рейтинг
                Geekplay.Instance.PlayerData.rate = serverData.GetValueOrDefault("rating", Geekplay.Instance.PlayerData.rate).ToInt();
                Geekplay.Instance.PlayerData.maxRate = serverData.GetValueOrDefault("bestRating", Geekplay.Instance.PlayerData.maxRate).ToInt();

                // 3. Валюта
                Geekplay.Instance.PlayerData.money = serverData.GetValueOrDefault("money", Geekplay.Instance.PlayerData.money).ToInt();
                Geekplay.Instance.PlayerData.donatMoney = serverData.GetValueOrDefault("donatMoney", Geekplay.Instance.PlayerData.donatMoney).ToInt();

                // 4. Клановая информация
                if (serverData.TryGetValue("clan", out var clanObj))
                {
                    if (clanObj is Newtonsoft.Json.Linq.JObject jClan)
                    {
                        Geekplay.Instance.PlayerData.clanId = jClan["id"]?.ToString() ?? Geekplay.Instance.PlayerData.clanId;
                        Geekplay.Instance.PlayerData.clanName = jClan["name"]?.ToString() ?? Geekplay.Instance.PlayerData.clanName;
                        Geekplay.Instance.PlayerData.clanPoints = jClan["points"]?.ToObject<int>() ?? Geekplay.Instance.PlayerData.clanPoints;
                    }
                    else
                    {
                        Debug.LogWarning("Clan data is not in JObject format");
                    }
                }

                // 5. Боевая статистика
                if (serverData.TryGetValue("stats", out var statsObj))
                {
                    var jStats = statsObj as Newtonsoft.Json.Linq.JObject;
                    if (jStats != null)
                    {
                        Geekplay.Instance.PlayerData.killOverral = jStats["overral_kill"]?.ToObject<int>() ?? Geekplay.Instance.PlayerData.killOverral;
                        Geekplay.Instance.PlayerData.winOverral = jStats["win_count"]?.ToObject<int>() ?? Geekplay.Instance.PlayerData.winOverral;
                        Geekplay.Instance.PlayerData.loseOverral = jStats["lose_count"]?.ToObject<int>() ?? Geekplay.Instance.PlayerData.loseOverral;
                        Geekplay.Instance.PlayerData.reviveAlly = jStats["revive_count"]?.ToObject<int>() ?? Geekplay.Instance.PlayerData.reviveAlly;
                        Geekplay.Instance.PlayerData.maxDamageBattle = jStats["max_damage"]?.ToObject<int>() ?? Geekplay.Instance.PlayerData.maxDamageBattle;
                        Geekplay.Instance.PlayerData.totalShots = jStats["shoot_count"]?.ToObject<int>() ?? Geekplay.Instance.PlayerData.totalShots;
                    }
                    else
                    {
                        Debug.LogWarning("Stats data is not in JObject format");
                    }
                }

                // 6. Герои и скины
                if (serverData.TryGetValue("characters", out var charactersObj))
                {
                    try
                    {
                        var jChars = charactersObj as Newtonsoft.Json.Linq.JObject;
                        if (jChars != null)
                        {
                            Array.Clear(Geekplay.Instance.PlayerData.openHeroes, 0, Geekplay.Instance.PlayerData.openHeroes.Length);

                            foreach (var heroEntry in jChars)
                            {
                                int heroId = GetHeroIdByName(heroEntry.Key);
                                if (heroId >= 0 && heroId < Geekplay.Instance.PlayerData.openHeroes.Length)
                                {
                                    Geekplay.Instance.PlayerData.openHeroes[heroId] = 1;
                                }
                            }

                            if (Geekplay.Instance.PlayerData.openHeroes.All(x => x == 0))
                            {
                                Geekplay.Instance.PlayerData.openHeroes[0] = 1;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing characters: {ex}");
                    }
                }

                // 7. Любимый герой
                if (serverData.TryGetValue("favoriteHero", out var favHero))
                {
                    Geekplay.Instance.PlayerData.favoriteHero = GetHeroIdByName(favHero.ToString());
                }

                // 8. Карты героев
                if (serverData.TryGetValue("hero_card", out var heroCardsObj))
                {
                    var jCards = heroCardsObj as Newtonsoft.Json.Linq.JObject;
                    if (jCards != null)
                    {
                        foreach (var hero in Geekplay.Instance.PlayerData.persons)
                        {
                            string heroName = GetHeroNameById(Array.IndexOf(Geekplay.Instance.PlayerData.persons, hero));
                            var token = jCards[heroName] ?? jCards[heroName.ToLower()];
                            if (token != null)
                            {
                                hero.heroCard = token.ToObject<int>();
                            }
                        }
                    }
                }

                // 9. Статистика по героям
                if (serverData.TryGetValue("hero_match", out var heroMatchObj))
                {
                    var jArray = heroMatchObj as Newtonsoft.Json.Linq.JArray;
                    if (jArray != null && jArray.Count == Geekplay.Instance.PlayerData.heroMatch.Length)
                    {
                        for (int i = 0; i < jArray.Count; i++)
                        {
                            Geekplay.Instance.PlayerData.heroMatch[i] = jArray[i].ToObject<int>();
                        }
                    }
                }

                // 10. Уровни героев
                if (serverData.TryGetValue("hero_levels", out var heroLevelsObj))
                {
                    var jArray = heroLevelsObj as Newtonsoft.Json.Linq.JArray;
                    if (jArray != null)
                    {
                        var levels = jArray.ToObject<List<Dictionary<string, object>>>();
                        Geekplay.Instance.PlayerData.LoadHeroLevels(levels);
                    }
                }

                // 11. Сохраняем изменения и обновляем UI
                Geekplay.Instance.Save();
                UpdateUI();

                Debug.Log("Server data loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading server data: {ex}");
            }
        });
    }

    public int GetHeroIdByName(string heroName)
    {
        // Обратная функция для GetHeroNameById
        Dictionary<string, int> heroIds = new Dictionary<string, int>
        {
            {"Kayel", 0}, {"Coco", 1}, {"Bobby", 2}, {"Mono", 3},
            {"Freddy", 4}, {"Ci-J", 5}, {"Zetta", 6}, {"Rambo", 7}
        };

        return heroIds.TryGetValue(heroName, out int id) ? id : 0;
    }

    private void UpdateUI()
    {
        if (favoriteHeroText != null)
        {
            int favoriteId = Geekplay.Instance.PlayerData.favoriteHero;
            favoriteHeroText.text = GetHeroNameById(favoriteId);
            Currency.Instance.UpdateAllTXT();
        }
        Rating.Instance.UpdateUI();
    }
}