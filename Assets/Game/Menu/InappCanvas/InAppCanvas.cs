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
    [SerializeField] private InAppSO[] specialInAppSOs;

    [Header("Panel_Gold")]
    [SerializeField] private Button[] goldButtons;
    [SerializeField] private RewardConfig[] goldReward;
    [SerializeField] private RewardSO goldRewardSO;
    [SerializeField] private InAppSO[] goldInAppSOs;

    [Header("Panel_Chest")]
    [SerializeField] private Button[] chestButtons;
    [SerializeField] private ChestConfigSO[] chestReward;
    [SerializeField] private InAppSO[] chestInAppSOs;

    [Header("Panel_Skins")]
    [SerializeField] private Button[] skinButtons;
    [SerializeField] private RewardConfig[] skinReward;
    [SerializeField] private ChestConfigSO skinChest;
    [SerializeField] private InAppSO[] skinInAppSOs;

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
        var playerData = Geekplay.Instance.PlayerData;

        bool allAreOne = playerData.persons.All(person =>
                  person.openSkinBody.All(skin => skin == 1));
        if (allAreOne)
        {
            skinButtons[0].interactable = false;
        }

        //-----------------Special buttons-----------------
        for (int i = 0; i < 3; i++)
        {
            var index = i;
            specialInAppSOs[i].Subscribe(() => ChestRewardCanvas.Instance.InitRewardsArray(specialReward[index].rewardConfigs, false));
            specialButtons[i].onClick.AddListener(() => specialInAppSOs[index].BuyItem());
            // specialButtons[i].onClick.AddListener(() => ChestRewardCanvas.Instance.InitRewardsArray(specialReward[index].rewardConfigs, false));
        }

        specialInAppSOs[3].Subscribe(() => ChestRewardCanvas.Instance.InitMultipleChests(chestsReward[0].rewardChestConfigs, false));
        specialButtons[3].onClick.AddListener(() => specialInAppSOs[3].BuyItem());
        // specialButtons[3].onClick.AddListener(() => ChestRewardCanvas.Instance.InitMultipleChests(chestsReward[0].rewardChestConfigs, false));
        //-----------------Special buttons-----------------

        //-----------------Gold-----------------
        goldRewardSO.Subscribe(() => ChestRewardCanvas.Instance.InitInstaReward(goldReward[0], false));
        goldButtons[0].onClick.AddListener(() => goldRewardSO.ShowRewardedAd());

        for (int i = 0; i < goldInAppSOs.Length; i++)
        {
            // Debug.Log(i);
            var index = i;
            goldInAppSOs[i].Subscribe(() => ChestRewardCanvas.Instance.InitInstaReward(goldReward[index+1], false));
            goldButtons[i+1].onClick.AddListener(() => goldInAppSOs[index].BuyItem());
            // goldButtons[i].onClick.AddListener(() => ChestRewardCanvas.Instance.InitInstaReward(goldReward[index], true));
        }
        //-----------------Gold-----------------

        //-----------------Chests-----------------
        for (int i = 0; i < chestButtons.Length; i++)
        {
            var index = i;
            chestInAppSOs[i].Subscribe(() => ChestRewardCanvas.Instance.InitChest(chestReward[index], true));
            chestButtons[i].onClick.AddListener(() => chestInAppSOs[index].BuyItem());
            // chestButtons[i].onClick.AddListener(() => ChestRewardCanvas.Instance.InitChest(chestReward[index], true));
        }
        //-----------------Chests-----------------

        //-----------------Skins-----------------
        skinInAppSOs[0].Subscribe(() => AllSkin(playerData));
        skinInAppSOs[1].Subscribe(() => ChestRewardCanvas.Instance.InitChest(skinChest, false));
        skinInAppSOs[2].Subscribe(() => rightPanelSkins.chooseHero.SetActive(true));

        skinButtons[0].onClick.AddListener(() => skinInAppSOs[0].BuyItem());
        // {
            // AllSkin(playerData);
        // });

        skinButtons[1].onClick.AddListener(() => skinInAppSOs[1].BuyItem());
        // skinButtons[1].onClick.AddListener(() => ChestRewardCanvas.Instance.InitChest(skinChest, false));

        skinButtons[2].onClick.AddListener(() => skinInAppSOs[2].BuyItem());
        // skinButtons[2].onClick.AddListener(() =>
        // {
        //     rightPanelSkins.chooseHero.SetActive(true);
        // });
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
        // WebSocketBase.Instance.
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

        foreach (var app in specialInAppSOs)
        {
            app.UnsubscribeAll();
        }
        foreach (var app in goldInAppSOs)
        {
            app.UnsubscribeAll();
        }
        foreach (var app in skinInAppSOs)
        {
            app.UnsubscribeAll();
        }
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