using System.Linq;
using UnityEngine;

public class GameEndStats : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private MyStats myStatsPrefab;
    [SerializeField] private OtherStats otherStatsPrefab;

    [Header("UI")]
    [SerializeField] private GameObject content;

    [Header("State")]
    [SerializeField] private bool isUpdate;

    public void InitStats(MatchEndResponse response)
    {
        if (isUpdate) return;
        isUpdate = true;

        var myName = Geekplay.Instance.PlayerData.name;

        var sorted = response.results
            .OrderBy(r => r.place)                   // 1-е место -> сверху
            .ThenByDescending(r => r.score)          // при равенстве места — больший счёт выше
            .ThenByDescending(r => r.kills)          // затем по киллам
            .ToList();

        foreach (var result in sorted)
        {
            if (result.player_name == myName) // лучше сравнивать по player_id, если есть
            {
                var row = Instantiate(myStatsPrefab, content.transform);
                row.Init(result);
            }
            else
            {
                var row = Instantiate(otherStatsPrefab, content.transform);
                row.Init(result);
            }
        }
    }
}