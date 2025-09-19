using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SpecialRewardArray
{
    public RewardConfig[] rewardConfigs;
}
[System.Serializable]
public class SpecialRewardChestArray
{
    public ChestConfigSO[] rewardChestConfigs = new ChestConfigSO[1];
}

public class InAppCanvas : MonoBehaviour
{
    public static InAppCanvas Instance;
    [Header("Referencess")]
    [SerializeField] private RightPanelSkins rightPanelSkins;
    [SerializeField] private GameObject[] rightPanels;

    [Header("Panel_Special")]
    [SerializeField] private Button[] specialButtons;
    [ShowInInspector] public SpecialRewardArray[] specialReward;
    [ShowInInspector] public SpecialRewardChestArray[] chestsReward = new SpecialRewardChestArray[1];
    [SerializeField] private int[] priceSpecial;

    [Header("Panel_Gold")]
    [SerializeField] private Button[] goldButtons;
    [SerializeField] private RewardConfig[] goldReward;
    [SerializeField] private RewardSO goldRewardSO;
    [SerializeField] private InAppSO[] goldInAppSOs;

    [Header("Panel_Chest")]
    [SerializeField] private Button[] chestButtons;
    [SerializeField] private ChestConfigSO[] chestReward;
    [SerializeField] private int[] priceChest;

    [Header("Panel_Skins")]
    [SerializeField] private Button[] skinButtons;
    [SerializeField] private RewardConfig[] skinReward;
    [SerializeField] private ChestConfigSO skinChest;
    [SerializeField] private int[] priceSkin;

    private void OnValidate()
    {
        // specialReward = new(1);
        // rewardChestConfigs = new [1];
        // chestsReward = new SpecialRewardChestArray[1];
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        bool allAreOne = Geekplay.Instance.PlayerData.persons.All(person =>
                  person.openSkinBody.All(skin => skin == 1));
        if (allAreOne)
        {
            skinButtons[0].interactable = false;
        }

        //-----------------Special buttons-----------------
        for (int i = 0; i < 3; i++)
        {
            var index = i;
            specialButtons[i].onClick.AddListener(() =>
            {
                if (Currency.Instance.SpendDonatMoney(priceSpecial[index]))
                {
                    ChestRewardCanvas.Instance.InitRewardsArray(specialReward[index].rewardConfigs, false);
                }
            });
        }
        specialButtons[3].onClick.AddListener(() =>
        {
            if (Currency.Instance.SpendDonatMoney(priceSpecial[3]))
                ChestRewardCanvas.Instance.InitMultipleChests(chestsReward[0].rewardChestConfigs, false);
        });


        //-----------------Gold-----------------
        goldRewardSO.Subscribe(() => ChestRewardCanvas.Instance.InitInstaReward(goldReward[0], false));
        goldButtons[0].onClick.AddListener(() => goldRewardSO.ShowRewardedAd());

        for (int i = 0; i < goldInAppSOs.Length; i++)
        {
            var index = i;
            goldInAppSOs[i].Subscribe(() => ChestRewardCanvas.Instance.InitInstaReward(goldReward[index + 1], false));
            goldButtons[i + 1].onClick.AddListener(() => goldInAppSOs[index].BuyItem());
        }
        //-----------------Gold-----------------

        //-----------------Chests-----------------
        for (int i = 0; i < chestButtons.Length; i++)
        {
            var index = i;

            chestButtons[i].onClick.AddListener(() =>
            {
                if (Currency.Instance.SpendDonatMoney(priceChest[index]))
                    ChestRewardCanvas.Instance.InitChest(chestReward[index], true);
            });
        }
        //-----------------Chests-----------------

        //-----------------Skins-----------------
        skinButtons[0].onClick.AddListener(() =>
        {
            if (Currency.Instance.SpendDonatMoney(priceSkin[0]))
                AllSkin(Geekplay.Instance.PlayerData);
        });

        skinButtons[1].onClick.AddListener(() => 
        {
            if (Currency.Instance.SpendDonatMoney(priceSkin[1]))
                skinButtons[1].onClick.AddListener(() => ChestRewardCanvas.Instance.InitChest(skinChest, false));
        });

        skinButtons[2].onClick.AddListener(() =>
        {
            if (Currency.Instance.SpendDonatMoney(priceSkin[2]))
                rightPanelSkins.chooseHero.SetActive(true);
        });
        //-----------------Skins-----------------
    }

    private void AllSkin(PlayerData playerData)
    {
        for (int i = 0; i < playerData.persons.Length; i++)
        {
            for (int j = 0; j < playerData.persons[i].openSkinBody.Length; j++)
            {
                playerData.persons[i].openSkinBody[j] = 1;
            }
        }
        skinButtons[0].interactable = false;
        Geekplay.Instance.Save();
        WebSocketBase.Instance.ClaimRewards(0, 0, new());
    }

    public void OpenSkins()
    {
        gameObject.SetActive(true);

        foreach (var panel in rightPanels)
        {
            panel.SetActive(false);
        }

        rightPanels[2].SetActive(true);
    }

    private void OnDestroy()
    {
        //---------------------------InApps---------------------------
        goldRewardSO.UnsubscribeAll();

        // foreach (var app in specialInAppSOs)
        // {
        //     app.UnsubscribeAll();
        // }
        foreach (var app in goldInAppSOs)
        {
            app.UnsubscribeAll();
        }
        // foreach (var app in skinInAppSOs)
        // {
        //     app.UnsubscribeAll();
        // }
        //---------------------------InApps---------------------------

        //---------------------------Buttons---------------------------
        foreach (var b in specialButtons)
        {
            b.onClick.RemoveAllListeners();
        }
        foreach (var b in goldButtons)
        {
            b.onClick.RemoveAllListeners();
        }
        foreach (var b in chestButtons)
        {
            b.onClick.RemoveAllListeners();
        }
        foreach (var b in skinButtons)
        {
            b.onClick.RemoveAllListeners();
        }
        //---------------------------Buttons---------------------------
    }
}