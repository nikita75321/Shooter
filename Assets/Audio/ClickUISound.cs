using UnityEngine;
using UnityEngine.EventSystems;

public class ClickUISound : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        InstanceSoundUI.Instance.PlayClickSound();
    }
}