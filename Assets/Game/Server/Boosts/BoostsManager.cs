using System;
using UnityEngine;

public enum BoostType
{
    ammo,
    armor,
    aidkit
}

[Serializable]
public class BoostList
{
    public Boost boost;
    public int id;
    public BoostType type;
    public bool isPickingUp = false;
}
public class BoostsManager : MonoBehaviour
{
    public static BoostsManager Instance;
    [SerializeField] private BoostList[] boostList;

    private void OnValidate()
    {
        if (boostList == null || boostList.Length == 0)
        {
            boostList = new BoostList[transform.childCount];

            for (int i = 0; i < transform.childCount; i++)
            {
                // Создаем новую структуру и заполняем ее
                BoostList newBoostList = new BoostList();

                Debug.Log("add");
                var index = i;
                newBoostList.boost = transform.GetChild(index).GetComponent<Boost>();
                newBoostList.boost.id = index;
                newBoostList.type = newBoostList.boost.type;
                newBoostList.id = index;

                // Присваиваем в массив
                boostList[i] = newBoostList;
            }
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        WebSocketBase.Instance.OnBoostPickupResponse += UpdateBoosts;
    }
    private void OnDisable()
    {
        WebSocketBase.Instance.OnBoostPickupResponse -= UpdateBoosts;
    }

    private void Start()
    {
        WebSocketBase.Instance.SendBoostsToServer(boostList);
    }

    public void PickUpBoost(int id)
    {
        var roomId = Geekplay.Instance.PlayerData.roomId;
        var playerId = Geekplay.Instance.PlayerData.id;

        WebSocketBase.Instance.SendBoostPickup(roomId, playerId, id);
    }

    public void UpdateBoosts(BoostPickupResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            var player = Level.Instance.currentLevel.player;

            switch (response.boost_type)
            {
                case "armor":
                    player.Character.Armor.ArmorIncrease(float.PositiveInfinity);
                    
                    break;
                case "health":
                    player.Character.currentHealthKits++;
                    // Обновляем UI через AidKit
                    AidKit aidKit = player.Character.Health.aidKit;
                    if (aidKit != null)
                    {
                        aidKit.UpdateKitText();
                        // Если достигли максимума, останавливаем заполнение
                        if (player.Character.currentHealthKits >= player.Character.maxHealthKits)
                        {
                            aidKit.StopFilling();
                        }
                        else if (!aidKit.isFilling)
                        {
                            aidKit.StartFilling();
                        }
                    }
                    break;
                case "ammo":
                    player.Character.AddAmmo();
                    break;
            }
        });
    }

    public void UpdateVisualBoosts(BoostTakenResponse response)
    {
        var curBoost = boostList[response.boost_id].boost;

        boostList[response.boost_id].isPickingUp = true;
        curBoost.isPickingUp = true;
        boostList[response.boost_id].boost.gameObject.SetActive(false);

        if (response.player_id == Geekplay.Instance.PlayerData.id)
        {
            var player = OnlineRoom.Instance.GetLocalPlayerInfo();
            player.armor = player.max_armor;
            Level.Instance.currentLevel.player.Character.Armor.ChangeArmor(player.armor);
        }
        else
        {
            var player = OnlineRoom.Instance.GetPlayerInfo(response.player_id);
            player.armor = player.max_armor;

            Enemy enemy = EnemiesInGame.Instance.GetEnemy(player.playerId);
            enemy.Armor.ChangeArmor(player.armor);
        }
    }
}