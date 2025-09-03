using UnityEngine;
using UnityEngine.Events;

public interface IGeekplay
{
	UnityEvent OnInterstitialStart { get; set; }
	UnityEvent OnInterstitialClose { get; set; }
	UnityEvent OnRewardAdStart { get; set; }
	UnityEvent OnRewardAdClose { get; set; }
	UnityEvent OnFocusOff { get; set; }
	UnityEvent OnFocusOn { get; set; }
	bool Mobile { get; set; }
	string YanValueType { get; set; }
	Leaderboard Leaderboard { get; set; }
	public void SetToLeaderboard(string leaderboardName, float value);
	string Language {get; set; }
	PlayerData PlayerData {get; set; }
	string PurchasedTag {get; set; }
	Platform Platform {get; set; }
	string RewardTag {get; set; }
	int LeaderNumberN {get; set;}
	int LeaderNumber {get; set;}
	string[] LS {get; }
	string[] LN {get; }
	OurGames OurGames {get; set; }

    void ShowInterstitialAd();
    void CreateClass(bool inapps, InAppSO[] purchases, RewardSO[] rewardsL, string yan, string lang, bool mob, PlayerData pd, Platform pl);
    void Save();
    void OnRewarded();
    void SetMyScore(int score);
    void SetMyPlace(int place);
    void GetLeadersScore(string value);
	void GetLeadersName(string value);
	void OnPurchasedItem();
	void CheckBuysOnStart(string idOrTag);
	void SetPurchasedItem();
	void NotSetPurchasedItem();
	void GameReady();
	void GameStart();
	void GameStop();
	void RateGame();
	void GamePlayed(int id);
	void GameNotPlayed(int id);
	void OpenAllGames(string uri);
	void OpenGame(string uri);
	void StopMusAndGame();
	void ResumeMusAndGame();
	void GoToGroup();
	void ToStarGame();
	void ShareGame();
	void InvitePlayers();
	void HappyTime();
	void ChangeSound();

}

public static class Geekplay
{
	public static Platform platform;

    public static IGeekplay Instance
    {
        get
        {
			if (platform == Platform.Yandex)
			{
			    return GeekplayYandex.Instance;
			}
			else if (platform == Platform.VK)
			{
			    return Geekplay_VK.Instance;
			}
			else if (platform == Platform.Editor)
			{
			    return GeekplayEditor.Instance;
			}
			else if (platform == Platform.GameDistribution)
			{
				return Geekplay_GameDistribution.Instance;
			}
			else
			{
			    return Geekplay_Crazy.Instance;
			}
        }
    }
}
