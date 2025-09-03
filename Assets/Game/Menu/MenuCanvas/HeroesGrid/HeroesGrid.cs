using UnityEngine;

public class HeroesGrid : MonoBehaviour
{
    [Header("Referencess")]
    [SerializeField] private HeroesCanvas heroesCanvas;
    [SerializeField] private HeroCardLock[] heroCardLock;

    [Header("Colors")]
    public Color[] colorsGlowText;
    public Color[] colorsGlow;
    public Color[] colorsBottomPanel;

    [Header("Card UI")]
    public HeroCard[] heroCards;
    public Sprite[] backgrounds;

    private void OnValidate()
    {
        // heroCardLock = GetComponentsInChildren<HeroCardLock>(true);
        heroCards = GetComponentsInChildren<HeroCard>(true);
    }

    public void InitGrid()
    {
        for (int i = 0; i < Geekplay.Instance.PlayerData.openHeroes.Length; i++)
        {
            int hero = Geekplay.Instance.PlayerData.openHeroes[i];
            if (hero == 1)
            {
                heroCardLock[i].HideLock();
                heroCards[i].InitCardRank(Geekplay.Instance.PlayerData.persons[i].rank);
            }
            else
            {
                heroCardLock[i].ShowLock();
                heroCards[i].InitLock();
                // heroCards[i].InitCardRank(0);
            }
        }
    }

    public void SelectHero(HeroCard heroCard)
    {
        heroesCanvas.gameObject.SetActive(true);
        heroesCanvas.InitChar(heroCard);
        
        gameObject.SetActive(false);
    }
}
