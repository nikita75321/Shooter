using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroSlotSkin : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Skins skins;
    public int id;
    [SerializeField] private Image iconSkin;
    [SerializeField] private RewardInChestSO skinArr;
    [SerializeField] private GameObject focusFrame, focusIcon;

    private void OnValidate()
    {
        id = transform.GetSiblingIndex();
        skins = FindAnyObjectByType<Skins>();
    }

    private void OnEnable()
    {
        // CheckCurSkin();
        // skins.buyButton.gameObject.SetActive(false);

        // if (Geekplay.Instance.PlayerData.persons[Geekplay.Instance.PlayerData.currentHero].openSkinBody[skins.curHeroId] == 1)
        // {
        //     skins.buyButton.gameObject.SetActive(false);
        // }
    }

    public void InitSlotsIcon()
    {
        iconSkin.sprite = skins.skinsIcons[skins.curHeroId].icon[id];
        iconSkin.SetNativeSize();
        iconSkin.rectTransform.sizeDelta *= 0.8f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var playerData = Geekplay.Instance.PlayerData;
        skins.buyButton.gameObject.SetActive(false);

        if (skins.currentSelectClot != this)
        {
            // Debug.Log(1);
            skins.SelectSlot(this);
        }
        else
        {
            // Debug.Log(2);
        }

        // if (id != 0)
        // {
        // Debug.Log("id - " + id);
        if (playerData.persons[skins.curHeroId].openSkinBody[id] == 1)
        {
            // Debug.Log(3);
            skins.selectSkinButton.gameObject.SetActive(true);
        }
        else
        {
            // Debug.Log(4);
            skins.selectSkinButton.gameObject.SetActive(false);
            skins.buyButton.gameObject.SetActive(true);
        }
            // }

        if (playerData.persons[skins.curHeroId].currentBody == id)
        {
            // Debug.Log(5);
            skins.selectSkinButton.gameObject.SetActive(false);
        }
    }

    public void SelectSlot()
    {
        focusFrame.SetActive(true);
        focusIcon.SetActive(true);
    }
    public void DeselectSlot()
    {
        focusFrame.SetActive(false);
        focusIcon.SetActive(false);
    }

    public void ShowPreview(HeroDummy dummy)
    {
        dummy.SelectSkin(id);
    }
}