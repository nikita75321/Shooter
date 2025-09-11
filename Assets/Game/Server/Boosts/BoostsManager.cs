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
        WebSocketBase.Instance.OnBoostPickupResponse += UpdateBoosts;
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
            
        });
    }

    public void UpdateVisualBoosts(BoostTakenResponse response)
    {
        Debug.Log(response.boost_id);
        var curBoost = boostList[response.boost_id].boost;

        boostList[response.boost_id].isPickingUp = true;
        curBoost.isPickingUp = true;
        boostList[response.boost_id].boost.gameObject.SetActive(false);
    }
}