using TMPro;
using UnityEngine;

public class MyStats : MonoBehaviour
{
    [SerializeField] private TMP_Text placeTXT;
    [SerializeField] private TMP_Text nameTXT;
    [SerializeField] private TMP_Text damageTXT;
    [SerializeField] private TMP_Text killsTXT;
    [SerializeField] private TMP_Text deathsTXT;

    public void Init(MatchPlayerResult result)
    {
        placeTXT.text = result.place.ToString();
        nameTXT.text = result.player_name.ToString();
        damageTXT.text = ((int)result.damage).ToString();
        killsTXT.text = result.kills.ToString();
        deathsTXT.text = result.deaths.ToString();
    }
}
