using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class SkinsIcon
{
    public Sprite[] icon;
}

public class Skins : MonoBehaviour
{
    [SerializeField] private HeroDummy currentHero;
    [SerializeField] private HeroDummy[] heroes;
    public SkinsIcon[] skinsIcons;
    public HeroSlotSkin[] heroSlots;
    public HeroSlotSkin currentSelectClot;
    public int curHeroId;

    [Header("Buttons skin")]
    public Button selectSkinButton;
    public Button buyButton;
    public Button selectHeroButton;

    [SerializeField] private GameObject buyButtonMoneyInfo, buyButtonDonatMoneyInf, buyButtonRewardInfo, buyButtonInAppInfo;

    [Header("PlayerLogo")]
    [SerializeField] private Image menuLogo;
    [SerializeField] private Image playerInfoLogo;

    private void OnValidate()
    {
        // if (heroSlots.Length == 0) heroSlots = GetComponentsInChildren<HeroSlotSkin>(true);
    }

    private void Start()
    {
        // var curSkinId = Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero].
        DOVirtual.DelayedCall(0.1f, () =>
        {
            var person = Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero];
            heroes[Geekplay.Instance.PlayerData.currentHero].SelectBody(person.currentBody);
            heroes[Geekplay.Instance.PlayerData.currentHero].SelectHead(person.currentHead);

            heroSlots[0].DeselectSlot();
            currentSelectClot = heroSlots[person.currentBody];
            currentSelectClot.SelectSlot();

            ChangeLogo(Geekplay.Instance.PlayerData.currentHero, person.currentBody);
        });
    } 

    public void InitSlots()
    {
        Debug.Log(2);
        if (currentSelectClot != null)
            currentSelectClot.DeselectSlot();

        var person = Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero];

        currentSelectClot = heroSlots[person.currentBody];
        // currentSelectClot.SelectSlot();

        // if (Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero].currentBody == id)
        // {
        // DOVirtual.DelayedCall(0.5f, () =>
        // {
        buyButton.gameObject.SetActive(false);
        selectSkinButton.gameObject.SetActive(false);
        Debug.Log(1);
        // });
        // }
    }

    public void SelectSkin()
    {
        selectSkinButton.gameObject.SetActive(false);

        var hero = Geekplay.Instance.PlayerData.persons[curHeroId];

        hero.currentBody = currentSelectClot.id;
        hero.currentHead = currentSelectClot.id;

        Geekplay.Instance.PlayerData.currentHeroBodySkin = currentSelectClot.id;
        Geekplay.Instance.PlayerData.currentHeroHeadSkin = currentSelectClot.id;

        Geekplay.Instance.Save();
    }

    public void BuySkin()
    {
        InAppCanvas.Instance.OpenSkins();
    }

    public void SelectHero(int id)
    {
        // Debug.Log("SelectHero "+ id);
        currentHero = heroes[id];
        var playerData = Geekplay.Instance.PlayerData;

        if (playerData.persons[playerData.currentHero].openSkinBody[currentSelectClot.id] == 1)
        {
            buyButton.gameObject.SetActive(false);
            // Debug.Log(1);
            if (playerData.persons[playerData.currentHero].currentBody == currentSelectClot.id)
            {
                // Debug.Log(2);
                selectHeroButton.gameObject.SetActive(true);
                selectSkinButton.gameObject.SetActive(false);
            }
            else
            {
                selectSkinButton.gameObject.SetActive(true);
                selectHeroButton.gameObject.SetActive(false);
                // Debug.Log(3);
            }
        }
        else
        {
            buyButton.gameObject.SetActive(true);
            // Debug.Log(4);
        }
    }

    public void SelectSlot(HeroSlotSkin heroSlot)
    {
        if (currentSelectClot != null)
        {
            currentSelectClot.DeselectSlot();
        }
        currentSelectClot = heroSlot;

        heroSlot.SelectSlot();
        heroSlot.ShowPreview(currentHero);

        selectSkinButton.gameObject.SetActive(true);
    }

    public void CheckCurSkin()
    {
        Debug.Log("CheckCurSkin, currentSelectClot.id - " + currentSelectClot.id);
        if (Geekplay.Instance.PlayerData.persons[curHeroId].openSkinBody[currentSelectClot.id] == 1)
        {
            Debug.Log("CheckCurSkin 1");
            buyButton.gameObject.SetActive(false);
            selectSkinButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("CheckCurSkin 2");
        }
    }

    public void ChangeLogo(int idHero, int idSkin)
    {
        menuLogo.sprite = skinsIcons[idHero].icon[idSkin];
        playerInfoLogo.sprite = skinsIcons[idHero].icon[idSkin];

        menuLogo.SetNativeSize();
        playerInfoLogo.SetNativeSize();
    }
}