using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroCard : MonoBehaviour
{
    [Header("Referencess")]
    [SerializeField] private HeroesGrid heroesGrid;

    public HeroData heroData;
    public HeroCardLock heroCardLock;

    [Header("UI")]
    [SerializeField] private Image back;
    [SerializeField] private Image bottomGlow;
    [SerializeField] private Image textRankGlow;
    [SerializeField] private Image BottomPanel;
    // [SerializeField] private Image[] stars;
    [SerializeField] private TMP_Text rankLetterTXT;
    [SerializeField] private Image iconWeapon;
    [SerializeField] private TMP_Text heroPowerTXT;

    private void OnValidate()
    {
        heroesGrid = GetComponentInParent<HeroesGrid>(true);
        // heroCardLock = GetComponentInChildren<HeroCardLock>(true);
    }

    public void SelectHero()
    {
        heroesGrid.SelectHero(this);
    }

    public void InitLock()
    {
        rankLetterTXT.enabled = false;
        textRankGlow.enabled = false;

        back.sprite = heroesGrid.backgrounds[0];
        bottomGlow.color = heroesGrid.colorsGlow[0];
    }
    public void InitCardRank(int value)
    {
        var index = transform.parent.GetSiblingIndex();
        var playerData = Geekplay.Instance.PlayerData;
        var curPerson = playerData.persons[index];

        rankLetterTXT.enabled = true;
        textRankGlow.enabled = true;

        // Debug.Log(value);
        switch (value)
        {
            case 1:
                rankLetterTXT.text = "D";
                break;
            case 2:
                rankLetterTXT.text = "C";
                break;
            case 3:
                rankLetterTXT.text = "B";
                break;
            case 4:
                rankLetterTXT.text = "A";
                break;
            case 5:
                rankLetterTXT.text = "S";
                break;
            case 6:
                rankLetterTXT.text = "SS";
                break;
        }

        back.sprite = heroesGrid.backgrounds[value - 1];
        bottomGlow.color = heroesGrid.colorsGlow[value - 1];
        BottomPanel.color = heroesGrid.colorsBottomPanel[value - 1];

        textRankGlow.color = heroesGrid.colorsGlowText[value - 1];
        rankLetterTXT.color = heroesGrid.colorsGlowText[value - 1] + new Color(10, 10, 10);

        iconWeapon.color = heroesGrid.colorsGlowText[value - 1];
        heroPowerTXT.color = heroesGrid.colorsGlowText[value - 1];

        heroPowerTXT.text = $"{Math.Round((HeroesCanvas.Instance.GetCurrentHealth(curPerson.level, curPerson.rank, index) + HeroesCanvas.Instance.GetCurrentArmor(curPerson.level, curPerson.rank, index) + HeroesCanvas.Instance.GetCurrentDamage(curPerson.level, curPerson.rank, index)) / 3)}";
    }
}
