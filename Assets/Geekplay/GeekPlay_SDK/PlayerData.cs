using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public bool SoundOn = true;

    [Title("SDK Parametrs")]
    public string lastBuy;
    public bool[] ourGamesRewards = new bool[4];
    public int[] promocodes = new int[5];
    public bool adInapp = false;
    public string saveLanguage = "en";

    [Title("Player Info")]
    public string id;
    public string name;
    public string clanId;
    public string clanName;
    public bool isClanLeader;

    [Title("Player Stats")]
    public int rate;
    public int maxRate;
    public int clanPoints;
    public int favoriteHero;
    public int[] heroMatch = { 0, 0, 0, 0, 0, 0, 0, 0 };
    public int killOverral;
    public int loseOverral;
    public int winOverral;
    public int reviveAlly;
    public int maxDamageBattle;
    public int totalShots;

    [Title("Menu Parametrs")]
    public string friendsReward;
    public int currentMode = 0;
    public bool isParty;
    public int money;
    public int donatMoney;
    public int[] openHeroes = { 1, 0, 0, 0, 0, 0, 0, 0 };
    public int currentHero = 0;
    public int currentHeroHeadSkin = 0;
    public int currentHeroBodySkin = 0;

    [Title("Rating Path")]
    public int[] ratingPathClaimReward = new int[28];

    [Title("Cases")]
    public int winCaseValue = 0;
    public int killCaseValue = 0;

    [Title("Heroes")]
    public Hero[] persons = new Hero[8];
    public void LoadHeroLevels(List<Dictionary<string, object>> levelsData)
    {
        for (int i = 0; i < Mathf.Min(levelsData.Count, persons.Length); i++)
        {
            persons[i].LoadFromLevelsJson(levelsData[i]);
        }
    }

    [Title("GAME Parametrs")] 
    public int version;
    public string roomId;
    public int killsCount;

    [Title("GAME SETTINGS")]
    public float masterVolume = 0.5f;
    public float musicVolume= 0.25f;
    public float sfxVolume= 0.5f;
    public float sensativity= 0.5f;
    public int graphics;
    
    public void ResetData()
    {
        Geekplay.Instance.PlayerData = new PlayerData();
        Geekplay.Instance.Save();
    }
}