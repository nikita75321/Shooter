using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class OurGames : MonoBehaviour
{
    [InfoBox("Заполните ID в SO.\nПри необходимости создайте еще SO других игр")]
    [TabGroup("Настройка SO")]
    public OurGames_SO[] ourGames;
    [TabGroup("Настройка UI")]
    [InfoBox("Добавьте больше UI при добавлении новых SO")]
    [ReadOnly]
    public Button[] gameBtns;
    [TabGroup("Настройка UI")]
    [ReadOnly]
    public Image[] icons;
    [TabGroup("Настройка UI")]
    [ReadOnly]
    public GameObject[] playGamesBtns;
    [TabGroup("Настройка UI")]
    [ReadOnly]
    public TextMeshProUGUI[] gameTexts;

    private string developerNameYandex = "GeeKid%20-%20школа%20программирования";

    void OnEnable()
    {
        //ourGames[0].Subscribe(YourRewardFunc);
        //ourGames[1].Subscribe(YourRewardFunc);
        //ourGames[2].Subscribe(YourRewardFunc);
        //ourGames[3].Subscribe(YourRewardFunc);
    }

    void OnDisable()
    {
        //ourGames[0].Unsubscribe(YourRewardFunc);
        //ourGames[1].Unsubscribe(YourRewardFunc);
        //ourGames[2].Unsubscribe(YourRewardFunc);
        //ourGames[3].Unsubscribe(YourRewardFunc);
    }

    void Start()
    {
    	Geekplay.Instance.OurGames = this;
    	if (Geekplay.Instance.Platform != Platform.Editor)
        {
            for (int i = 0; i < ourGames.Length; i++)
            {
                CheckPlayedGame(ourGames[i].gameId);
                icons[i].sprite = ourGames[i].icon;
            }
        }
    }

    public void CheckPlayedGame(int id)
    {
        Utils.CheckPlayGame(id);
    }

    public void OpenOtherGames() //ССЫЛКА НА ДРУГИЕ ИГРЫ
        {
            switch (Geekplay.Instance.Platform)
            {
                case Platform.Editor:
                    #if INIT_DEBUG
                        Debug.Log($"<color=yellow>OPEN OTHER GAMES</color>");
                    #endif
                    break;
                case Platform.Yandex:
                    Utils.GetAllGames();
                    //var domain = Utils.GetDomain();
                    //Application.OpenURL($"https://yandex.{domain}/games/developer?name=" + developerNameYandex);
                    break;
            }
        }

    public void OpenGame(int appID)
        {
            switch (Geekplay.Instance.Platform)
            {
                case Platform.Editor:

                    Debug.Log($"<color={Color.yellow}>OPEN OTHER GAMES</color>");

                    break;
                case Platform.Yandex:
                    Utils.GetGameByID(appID);
                    //var domain = Utils.GetDomain();
                    //Application.OpenURL($"https://yandex.{domain}/games/#app={appID}");
                    break;
            }
        }

    public void GamePlayed(int number)
    {
    	if (!Geekplay.Instance.PlayerData.ourGamesRewards[number])
                {
                    gameBtns[number].onClick.RemoveAllListeners();
                    gameBtns[number].interactable = true;
                    gameBtns[number].onClick.AddListener(() =>
                    {
                        TakeReward(number, gameBtns[number], gameTexts[number]);
                        Geekplay.Instance.PlayerData.ourGamesRewards[number] = true;
                        Geekplay.Instance.Save();
                    });

                    if (Geekplay.Instance.Language == "ru")
                    {
                        gameTexts[number].text = "Забрать";
                    }
                    else if (Geekplay.Instance.Language == "en")
                    {
                        gameTexts[number].text = "Claim";
                    }
                    else if (Geekplay.Instance.Language == "tr")
                    {
                        gameTexts[number].text = "Iddia";
                    }
                }
                else
                {
                    gameBtns[number].interactable = false;

                    if (Geekplay.Instance.Language == "ru")
                    {
                        gameTexts[number].text = "Забрали";
                    }
                    else if (Geekplay.Instance.Language == "en")
                    {
                        gameTexts[number].text = "Claimed";
                    }
                    else if (Geekplay.Instance.Language == "tr")
                    {
                        gameTexts[number].text = "talep edildi";
                    }
                }
            playGamesBtns[number].SetActive(true);
            Geekplay.Instance.Save();
    }

    public void GameNotPlayed(int number)
    {
    	gameBtns[number].onClick.RemoveAllListeners();
            gameBtns[number].interactable = true;
            gameBtns[number].onClick.AddListener(() =>
            {
                OpenGame(ourGames[number].gameId);
            });

            if (Geekplay.Instance.Language == "ru")
            {
                gameTexts[number].text = "Играть";
            }
            else if (Geekplay.Instance.Language == "en")
            {
                gameTexts[number].text = "Play";
            }
            else if (Geekplay.Instance.Language == "tr")
            {
                gameTexts[number].text = "Oynamak";
            }
            Geekplay.Instance.Save();
    }

    public void TakeReward(int number, Button button, TextMeshProUGUI text)
        {
            ourGames[number].TakeReward();
            button.interactable = false;

            if (Geekplay.Instance.Language == "ru")
            {
                text.text = "Получено";
            }
            else if (Geekplay.Instance.Language == "en")
            {
                text.text = "Taked";
            }
            else if (Geekplay.Instance.Language == "tr")
            {
                text.text = "Alindi";
            }
        }
}
