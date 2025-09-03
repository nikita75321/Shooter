using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "OurGames", menuName = "Geekplay/OurGames")]
public class OurGames_SO : ScriptableObject
{
    [PreviewField(60), HideLabel]
    [HorizontalGroup("Split", 60)]
    public Sprite icon;

    [VerticalGroup("Split/Right"), LabelWidth(120)]
    public int gameId;
    //[VerticalGroup("Split/Right"), LabelWidth(120)]
    //public string name;
    //[VerticalGroup("Split/Right"), LabelWidth(120)]
    [HideInInspector]
    public UnityEvent rewardEvent;

    public void Subscribe(UnityAction action)
    {
    	rewardEvent.AddListener(action);
    }

    public void Unsubscribe(UnityAction action)
    {
    	rewardEvent.RemoveListener(action);
    }

    public void TakeReward() 
    {
        rewardEvent.Invoke();
    }
}