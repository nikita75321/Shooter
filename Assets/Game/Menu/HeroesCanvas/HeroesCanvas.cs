using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroesCanvas : MonoBehaviour
{
    public static HeroesCanvas Instance;

    [Header("Referencess")]
    [SerializeField] private Level level;
    [SerializeField] private Skins skins;
    [SerializeField] private GameObject[] allHeroes;

    [Header("Hero info")]
    [SerializeField] private HeroData curHeroData;
    [SerializeField] private TMP_Text heroName;
    [SerializeField] private TMP_Text heroClass;
    [SerializeField] private TMP_Text heroLevel;
    [SerializeField] private bool keepLevelAfterRankUp = true;
    // [SerializeField] private TMP_Text heroRank;

    [SerializeField] private GameObject[] rarityPanel;

    [SerializeField] private TMP_Text heroPower;
    [SerializeField] private TMP_Text heroHealth;
    [SerializeField] private TMP_Text heroDamage;
    [SerializeField] private TMP_Text heroArmor;
    [SerializeField] private Button moneyButton;
    [SerializeField] private Button rankButton;
    [SerializeField] private Button maxLevelButton;

    [Header("Level up")]
    [SerializeField] private Image[] cardImage;
    [SerializeField] private TMP_Text cardInfo;
    [SerializeField] private TMP_Text moneyToLevelUp;

    [Header("LevelUp panel")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TMP_Text curLevel, lvlupLevel;
    [SerializeField] private TMP_Text curHelth, lvlupHelth;
    [SerializeField] private TMP_Text curArmor, lvlupArmor;
    [SerializeField] private TMP_Text curDamage, lvlupDamage;
    [SerializeField] private TMP_Text moneyLvlUpTXT;
    [SerializeField] private Image[] cardImageUp;
    [SerializeField] private TMP_Text cardInfoUp;

    [SerializeField] private Button moneyButtonUp;
    [SerializeField] private Button rankButtonUp;

    [Header("Sliders")]
    [SerializeField] private Slider slider, sliderUp;

    [Header("Sliders stats")]
    [SerializeField] private Slider sliderPower;
    [SerializeField] private Slider sliderHp;
    [SerializeField] private Slider sliderDamage;
    [SerializeField] private Slider sliderArmor;

    [SerializeField] private int maxPower;
    [SerializeField] private int maxHp;
    [SerializeField] private int maxDamage;
    [SerializeField] private int maxArmor;

    [Header("Description rank up")]
    [SerializeField] private TMP_Text infoRankTXT;

    public void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //Значения для слайдеров
        maxPower = 19054;
        maxHp = 35145;
        maxDamage = 9549;
        maxArmor = 15915;

        sliderPower.maxValue = maxPower;
        sliderHp.maxValue = maxHp;
        sliderDamage.maxValue = maxDamage;
        sliderArmor.maxValue = maxArmor;
    }

    private void OnEnable()
    {
        // Debug.Log(1);
        // skins.InitSlots();
    }

    public void InitChar(HeroCard heroCard)
    {
        curHeroData = heroCard.heroData;

        foreach (var hero in allHeroes)
        {
            hero.SetActive(false);
        }

        allHeroes[heroCard.heroData.id].SetActive(true);
        skins.SelectHero(heroCard.heroData.id);

        heroName.text = heroCard.heroData.heroName;
        heroClass.text = $"Класс героя: <color=white> {heroCard.heroData.heroClass}";

        if (curHeroData.heroRarity == Rarity.common)
            rarityPanel[0].SetActive(true);

        foreach (var go in rarityPanel)
        {
            go.SetActive(false);
        }
        rarityPanel[(int)curHeroData.heroRarity].SetActive(true);

        foreach (var icon in cardImage)
        {
            icon.gameObject.SetActive(false);
        }
        cardImage[heroCard.heroData.id].gameObject.SetActive(true);

        var curPerson = Geekplay.Instance.PlayerData.persons[heroCard.heroData.id];
        cardInfo.text = $"{curPerson.heroCard}/{GetRequiredCardsForLevel(curPerson.level)}";
        cardInfoUp.text = $"{curPerson.heroCard}/{GetRequiredCardsForLevel(curPerson.level)}";

        UpdateRankButtons(curPerson.level);
        UpdateUICharInfo();

        if (curPerson.level == 50 && curPerson.rank == 6)
        {
            maxLevelButton.gameObject.SetActive(true);
            infoRankTXT.gameObject.SetActive(false);
            cardInfo.text = "MAX";
        }
        else
        {
            infoRankTXT.gameObject.SetActive(true);
            maxLevelButton.gameObject.SetActive(false);
        }
        // UpdateSliders(curPerson.level, curPerson.rank);

        UpdateRankAndLevelUI(curPerson.level, curPerson.rank);

        skins.curHeroId = heroCard.heroData.id;
        // Debug.Log(heroCard.heroData.id + " heroCard.heroData.id");
        skins.SelectSlot(skins.heroSlots[Geekplay.Instance.PlayerData.persons[heroCard.heroData.id].currentBody]);
        skins.selectSkinButton.gameObject.SetActive(false);
        foreach (var slot in skins.heroSlots)
        {
            slot.InitSlotsIcon();
        }

        UpdateSliderStats();
    }

    private void UpdateSliderStats()
    {
        // Debug.Log("UpdateSliderStats");
        var person = Geekplay.Instance.PlayerData.persons[curHeroData.id];
        // Debug.Log(person + " "+ person.level + " " + person.rank);

        float currentHealth = GetCurrentHealth(person.level, person.rank);
        float currentArmor = GetCurrentArmor(person.level, person.rank);
        float currentDamage = GetCurrentDamage(person.level, person.rank);

        // Debug.Log(currentHealth + " " + currentArmor + " " + currentDamage);
        sliderPower.value = (currentHealth + currentArmor + currentDamage) / 3;
        sliderHp.value = currentHealth;
        sliderDamage.value = currentArmor;
        sliderArmor.value = currentDamage;
    }

    private void UpdateRankAndLevelUI(int level, int rank)
    {
        heroLevel.text = $"LEVEL <size=57.5>{level}";
        // heroRank.text = $"RANK <size=57.5>{rank}";
    }

    private bool IsRankUpCompleted(int level, int rank)
    {
        if (!keepLevelAfterRankUp) return false;

        // Для уровня 10 - ранг должен быть 1
        // Для уровня 20 - ранг должен быть 2 и т.д.
        int expectedRank = level / 10;
        return rank >= expectedRank;
    }

    private void UpdateRankButtons(int level)
    {
        bool isRankLevel = (level % 10 == 0) && (level < 50);
        moneyButton.gameObject.SetActive(!isRankLevel);
        rankButton.gameObject.SetActive(isRankLevel);
        moneyButtonUp.gameObject.SetActive(!isRankLevel);
        rankButtonUp.gameObject.SetActive(isRankLevel);
    }
    private void UpdateRankButtons(int level, int rank)
    {
        // Проверяем, был ли уже повышен ранг на этом уровне
        bool isRankAlreadyUp = rank > (level / 10);
        bool isMaxRank = rank > 5;
        bool isRankLevel = (level % 10 == 0) && !isRankAlreadyUp && !isMaxRank;

        moneyButton.gameObject.SetActive(!isRankLevel || isRankAlreadyUp);
        rankButton.gameObject.SetActive(isRankLevel);
        moneyButtonUp.gameObject.SetActive(!isRankLevel || isRankAlreadyUp);
        rankButtonUp.gameObject.SetActive(isRankLevel);
        maxLevelButton.gameObject.SetActive(isMaxRank);
    }

    private int GetRequiredCardsForLevel(int currentLevel)
    {
        if (currentLevel <= 10) return 25;
        else if (currentLevel <= 20) return 50;
        else if (currentLevel <= 30) return 75;
        else if (currentLevel <= 40) return 100;
        else if (currentLevel <= 50) return 125;
        else return currentLevel * 5;
    }
    private int GetRequiredCardsForRank(int rank)
    {
        switch(rank)
        {
            case 1: return 25;
            case 2: return 50;
            case 3: return 75;
            case 4: return 100;
            case 5: return 125;
            default: return 999;
        }
    }
    private int GetRequiredLevel(int currentLevel)
    {
        if (currentLevel < 10) return 10;
        else if (currentLevel < 20) return 20;
        else if (currentLevel < 30) return 30;
        else if (currentLevel < 40) return 40;
        else if (currentLevel < 50) return 50;
        else return currentLevel * 5;
    }
    private int GetRequiredRank(int curretLevel)
    {
        if (curretLevel < 10) return 1;
        else if (curretLevel < 20) return 2;
        else if (curretLevel < 30) return 3;
        else if (curretLevel < 40) return 4;
        else if (curretLevel < 50) return 5;
        else return curretLevel * 5;
    }

    public void SelectChar()
    {
        Geekplay.Instance.PlayerData.currentHero = curHeroData.id;
        // Geekplay.Instance.PlayerData.currentHeroBodySkin = curHeroData
        Geekplay.Instance.Save();
        Debug.Log($"HeroId - {curHeroData.id}, HeroSkin - {Geekplay.Instance.PlayerData.currentHeroBodySkin}");
        skins.ChangeLogo(curHeroData.id, Geekplay.Instance.PlayerData.persons[curHeroData.id].currentBody);
    }

    public void CheckCurHero()
    {
        foreach (var hero in allHeroes)
        {
            hero.SetActive(false);
        }

        var playerData = Geekplay.Instance.PlayerData;
        allHeroes[playerData.currentHero].SetActive(true);

        skins.SelectSlot(skins.heroSlots[playerData.persons[playerData.currentHero].currentBody]);
    }

    public void OpenLevelUpPanel()
    {
        levelUpPanel.SetActive(true);
        UpdateUILevelUp();
    }

    private void CloseLevelUpPanel()
    {
        levelUpPanel.SetActive(false);
        UpdateUICharInfo();
    }

    private void UpdateUICharInfo()
    {
        var person = Geekplay.Instance.PlayerData.persons[curHeroData.id];

        UpdateRankAndLevelUI(person.level, person.rank);
        moneyToLevelUp.text = (person.level * 100).ToString();

        float currentHealth = GetCurrentHealth(person.level, person.rank);
        float currentArmor = GetCurrentArmor(person.level, person.rank);
        float currentDamage = GetCurrentDamage(person.level, person.rank);

        heroPower.text = ((currentHealth + currentArmor + currentDamage) / 3).ToString("0");
        heroHealth.text = currentHealth.ToString("0");
        heroArmor.text = currentArmor.ToString("0");
        heroDamage.text = currentDamage.ToString("0");

        UpdateRankButtons(person.level, person.rank);

        bool isRankAlreadyUp = person.rank > (person.level / 10);
        // Debug.Log(person.rank);
        // Debug.Log(isRankAlreadyUp);
    
        if (person.rank > 5 && person.level == 50)
        {
            infoRankTXT.text = "Максимальный уровень и ранг достигнуты";
        }
        else if (!isRankAlreadyUp && person.rank < 6)
        {
            infoRankTXT.text = $"Требуется {GetRequiredCardsForRank(person.rank)} карт для повышения ранга";
        }
        else
        {
            infoRankTXT.text = $"Тренируйте героя до {GetRequiredLevel(person.level)} уровня, чтобы повысить ранг";
        }

        UpdateSliders(person.level, person.rank);
    }

    private void UpdateUILevelUp()
    {
        var person = Geekplay.Instance.PlayerData.persons[curHeroData.id];
        bool isRankLevel = (person.level % 10 == 0) && (person.rank < 5);

        float currentHealth = GetCurrentHealth(person.level, person.rank);
        float currentArmor = GetCurrentArmor(person.level, person.rank);
        float currentDamage = GetCurrentDamage(person.level, person.rank);

        curLevel.text = $"Level {person.level}";
        curHelth.text = currentHealth.ToString("0");
        curArmor.text = currentArmor.ToString("0");
        curDamage.text = currentDamage.ToString("0");

        if (person.rank < GetRequiredRank(person.level))
        {
            // Для ранга показываем увеличение на 15%
            lvlupLevel.text = $"Rank {person.rank + 1}";
            lvlupHelth.text = (currentHealth * 1.15f).ToString("0");
            lvlupArmor.text = (currentArmor * 1.15f).ToString("0");
            lvlupDamage.text = (currentDamage * 1.15f).ToString("0");
        }
        else
        {
            // Для обычного уровня показываем увеличение на 5%
            lvlupLevel.text = $"Level {person.level + 1}";
            lvlupHelth.text = (currentHealth * 1.05f).ToString("0");
            lvlupArmor.text = (currentArmor * 1.05f).ToString("0");
            lvlupDamage.text = (currentDamage * 1.05f).ToString("0");
        }

        moneyLvlUpTXT.text = (person.level * 100).ToString();
        UpdateRankButtons(person.level, person.rank);

        foreach (var icon in cardImageUp)
        {
            icon.gameObject.SetActive(false);
        }
        cardImageUp[curHeroData.id].gameObject.SetActive(true);

        if (person.rank <= 5)
        {
            cardInfo.text = $"{person.heroCard}/{GetRequiredCardsForRank(person.rank)}";
            cardInfoUp.text = $"{person.heroCard}/{GetRequiredCardsForRank(person.rank)}";
        }
        else
        {
            cardInfo.text = "MAX";
            cardInfoUp.text = "MAX";
        }

        UpdateSliders(person.level, person.rank);
    }

    #region GetCurrentStats
    public float GetCurrentHealth(int level, int rank)
    {
        float health = curHeroData.health;
        // Умножаем на множитель ранга (15% за каждый ранг)
        health *= Mathf.Pow(1.15f, rank);
        // Умножаем на множитель уровня (5% за каждый уровень)
        health *= Mathf.Pow(1.05f, level);
        return health;
    }

    public float GetCurrentArmor(int level, int rank)
    {
        float armor = curHeroData.armor;
        armor *= Mathf.Pow(1.15f, rank);
        armor *= Mathf.Pow(1.05f, level);
        return armor;
    }

    public float GetCurrentDamage(int level, int rank)
    {
        float damage = curHeroData.damage;
        damage *= Mathf.Pow(1.15f, rank);
        damage *= Mathf.Pow(1.05f, level);
        return damage;
    }

    public float GetCurrentHealth(int level, int rank, int id)
    {
        float health = this.level.heroDatas[id].health;

        health *= Mathf.Pow(1.15f, rank);

        health *= Mathf.Pow(1.05f, level);
        return health;
    }
    public float GetCurrentArmor(int level, int rank, int id)
    {
        float armor = this.level.heroDatas[id].armor;

        armor *= Mathf.Pow(1.15f, rank);

        armor *= Mathf.Pow(1.05f, level);
        return armor;
    }
    public float GetCurrentDamage(int level, int rank, int id)
    {
        float damage = this.level.heroDatas[id].damage;

        damage *= Mathf.Pow(1.15f, rank);

        damage *= Mathf.Pow(1.05f, level);
        return damage;
    }

    private void UpdateSliders(int level, int rank)
    {
        // Debug.Log("UpdateSliders");
        var person = Geekplay.Instance.PlayerData.persons[curHeroData.id];
        int currentCards = person.heroCard;

        // int requiredCards = person.level % 10 == 0 && person.rank < 5 ?
        //     GetRequiredCardsForRank(person.rank + 1) : GetRequiredCardsForRank(person.rank + 1);
        int requiredCards = GetRequiredCardsForRank(person.rank);

        float progress = requiredCards > 0 ?
            Mathf.Clamp01((float)currentCards / requiredCards) : 0;

        // Debug.Log($"{person.heroCard} - {requiredCards}");
        slider.value = progress;
        sliderUp.value = progress;

        if (rank > 5)
            slider.value = 1f;
    }
    #endregion

    public void TryLevelUp()
    {
        var person = Geekplay.Instance.PlayerData.persons[curHeroData.id];
        if (person.rank > 5)
        {
            Debug.Log("1");
            return;
        }
        else
        {
            Debug.Log(2);
        }
        if (Currency.Instance.SpendMoney(person.level * 100))
        {
            person.level += 1;

            if (levelUpPanel.activeSelf)
            {
                UpdateUILevelUp();
            }
            UpdateUICharInfo();
            UpdateSliders(person.level, person.rank);
            UpdateSliderStats();
            WebSocketBase.Instance.UpdateHeroLevels(curHeroData.id);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

    public void TryRankUp()
    {
        var person = Geekplay.Instance.PlayerData.persons[curHeroData.id];
        if (person.level % 10 != 0 || person.rank > 5) return;

        int requiredCards = GetRequiredCardsForRank(person.rank);
        if (HeroCards.Instance.SpendCard(requiredCards, curHeroData.id))
        {
            person.rank += 1;

            // После повышения ранга сразу обновляем кнопки
            UpdateRankButtons(person.level, person.rank);

            if (levelUpPanel.activeSelf)
            {
                UpdateUILevelUp();
            }
            UpdateUICharInfo();
            UpdateSliders(person.level, person.rank);

            if (person.rank > 5)
            {
                CloseLevelUpPanel();
            }
            WebSocketBase.Instance.UpdateHeroLevels(curHeroData.id);
        }
        else
        {
            Debug.Log("Not enough cards");
        }
    }

    public void MaxLevel()
    {
        Debug.Log("MAX LEVEL !!!");
    }
}