using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardSlot : MonoBehaviour
{
    [Header("UI Referencess")]
    [SerializeField] private Image placeIcon;
    [SerializeField] private TMP_Text placeTXT;
    [SerializeField] private TMP_Text nameTXT;
    [SerializeField] private TMP_Text retingTXT;

    private void Start()
    {

    }

    public void Init(string place, string name, string value)
    {
        Debug.Log(place);
        if (place.ToInt() < 4 && place.ToInt() > 0)
        {
            // placeIcon.enabled = true;
            placeIcon.sprite = ClanCanvas.Instance.placeSprites[place.ToInt() - 1];
        }
        else
        {
            placeIcon.enabled = false;
        }

        placeTXT.text = place;
        nameTXT.text = name;
        retingTXT.text = value;
    }

    private void Update()
    {
        
    }
}
