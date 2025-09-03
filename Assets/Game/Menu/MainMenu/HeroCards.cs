using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class HeroCards : MonoBehaviour
{
    public static HeroCards Instance;
    [ShowInInspector] public Dictionary<int, Hero> heroes = new(8);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < Geekplay.Instance.PlayerData.persons.Length; i++)
        {
            // Debug.Log(i);
            Hero hero = Geekplay.Instance.PlayerData.persons[i];
            heroes.Add(i, hero);
        }
    }

    public void AddCard(int value)
    {
        // Debug.Log($"AddCard {value}");
        Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero].heroCard += value;
        Geekplay.Instance.Save();
    }
    public void AddCardHero(int id, int value)
    {
        // Debug.Log($"AddCardHero id-{id} :{value}");
        Geekplay.Instance.PlayerData.persons[id].heroCard += value;
        Geekplay.Instance.Save();
    }

    public bool SpendCard(int value, int id)
    {
        // Debug.Log(value);
        // Debug.Log(Geekplay.Instance.PlayerData.persons[id].level + " - Level");
        // Debug.Log(Geekplay.Instance.PlayerData.persons[id].heroCard + " " + value);
        if (Geekplay.Instance.PlayerData.persons[id].heroCard >= value)
        {
            // Debug.Log(1);
            Geekplay.Instance.PlayerData.persons[id].heroCard -= value;
            Geekplay.Instance.Save();

            var data = new Dictionary<int, int> { { id, value } };
            WebSocketBase.Instance.SpendHeroCards(data);
            
            return true;
        }
        // Debug.Log(2);
        return false;
    }
}
