using System;
using UnityEngine;

public enum UpgradeType
{
    aim,
    armor,
    noize,
    damage,
    magazine,
    vision
}

[Serializable]
public class UpgradesList
{
    public Upgrade upgrade;
    public int id;
    public UpgradeType type;
    public bool isPickingUp = false;
}

public class UpgradesManager : MonoBehaviour
{
    public static UpgradesManager Instance;
    public UpgradesList[] upgradeList;

    private void OnValidate()
    {
        if (upgradeList == null || upgradeList.Length == 0)
        {
            upgradeList = new UpgradesList[transform.childCount];

            for (int i = 0; i < transform.childCount; i++)
            {
                // Создаем новую структуру и заполняем ее
                UpgradesList newUpgradeList = new UpgradesList();

                Debug.Log("add");
                var index = i;
                newUpgradeList.upgrade = transform.GetChild(index).GetComponent<Upgrade>();
                newUpgradeList.upgrade.id = index;
                newUpgradeList.type = newUpgradeList.upgrade.type;
                newUpgradeList.id = index;

                // Присваиваем в массив
                upgradeList[i] = newUpgradeList;
            }
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        WebSocketBase.Instance.OnUpgradePickupResponse += UpdateUpgrades;
        WebSocketBase.Instance.OnUpgradeTaken += UpdateVisualUpgrades;
        WebSocketBase.Instance.OnUpgradeDropped += HandleUpgradeDropped;

        WebSocketBase.Instance.OnMatchStart += SpawnUpgrade;
    }

    private void OnDisable()
    {
        WebSocketBase.Instance.OnUpgradePickupResponse -= UpdateUpgrades;
        WebSocketBase.Instance.OnUpgradeTaken -= UpdateVisualUpgrades;
        WebSocketBase.Instance.OnUpgradeDropped -= HandleUpgradeDropped;

        WebSocketBase.Instance.OnMatchStart -= SpawnUpgrade;
    }

    private void SpawnUpgrade(MatchStartResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            WebSocketBase.Instance.SendUpgradesToServer(upgradeList);
        });
    }

    public void PickUpUpgrade(int id)
    {
        var roomId = Geekplay.Instance.PlayerData.roomId;
        var playerId = Geekplay.Instance.PlayerData.id;

        WebSocketBase.Instance.SendUpgradePickup(roomId, playerId, id);
    }

    public void UpdateUpgrades(UpgradePickupResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log($"Upgrade picked up: {response.upgrade_type}");

            var player = Level.Instance.currentLevel.player;
            switch (response.upgrade_type)
            {
                case "aim":
                    
                    break;
                case "armor":

                    break;
                case "noize":
                    
                    break;
                case "damage":
                    
                    break;
                case "magazine":
                    
                    break;
                case "vision":
                    
                    break;
            }
        });
    }

    public void UpdateVisualUpgrades(UpgradeTakenResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            var curUpgrade = upgradeList[response.upgrade_id].upgrade;

            upgradeList[response.upgrade_id].isPickingUp = true;
            curUpgrade.isPickingUp = true;
            curUpgrade.gameObject.SetActive(false);
        });
    }

    public void DropUpgrade(Vector3 position, int id)
    {
        upgradeList[id].isPickingUp = false;

        var upgrade = upgradeList[id].upgrade;

        upgrade.transform.position = position;
        upgrade.isPickingUp = false;
        upgrade.gameObject.SetActive(true);
        
        WebSocketBase.Instance.SendUpgradeDrop(position, id);
    }

    public void HandleUpgradeDropped(UpgradeDroppedResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log("Spawn drop upgrade");
            var curUpgrade = upgradeList[response.upgrade_id].upgrade;

            upgradeList[response.upgrade_id].isPickingUp = false;
            curUpgrade.isPickingUp = false;
            curUpgrade.transform.position = response.position.ToVector3();
            curUpgrade.gameObject.SetActive(true);
        });
    }
}