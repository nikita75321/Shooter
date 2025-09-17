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
        if (!isUpdate)
        {
            isUpdate = true;
            
            for (int i = 0; i < response.results.Count; i++)
            {
                var result = response.results[i];
                if (result.player_name == Geekplay.Instance.PlayerData.name)
                {
                    myStatsPrefab.Init(result);
                }
                else
                {
                    var other = Instantiate(otherStatsPrefab, content.transform);
                    other.Init(result);
                }
            }
        }
    }
}
