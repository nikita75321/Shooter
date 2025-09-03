using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using DG.Tweening;

public class TG_System : MonoBehaviour
{
	[InfoBox("Заполните настройки промокодов в созданных SO\nПри необходимости создайте еще промокоды SO")]
	[SerializeField] private TG_SO[] promocodes;
	[ReadOnly]
	[SerializeField] private Image rewardImage;
	[ReadOnly]
	[SerializeField] private GameObject rewardPanel;
	[ReadOnly]
	[SerializeField] private TextMeshProUGUI rewardNameText;
	[ReadOnly]
	[SerializeField] private TMP_InputField inputField;
	[ReadOnly]
	[SerializeField] private TextMeshProUGUI warningText;
	IEnumerator f_cor;
	int chosen;

	void OnEnable()
	{
		//promocodes[0].Subscribe(YourFunctionReward);
		//promocodes[1].Subscribe(YourFunctionReward);
		//promocodes[2].Subscribe(YourFunctionReward);
		//promocodes[3].Subscribe(YourFunctionReward);
		//promocodes[4].Subscribe(YourFunctionReward);
	}

	void OnDisable()
	{
		//promocodes[0].Unsubscribe(YourFunctionReward);
		//promocodes[1].Unsubscribe(YourFunctionReward);
		//promocodes[2].Unsubscribe(YourFunctionReward);
		//promocodes[3].Unsubscribe(YourFunctionReward);
		//promocodes[4].Unsubscribe(YourFunctionReward);
	}

    public void OpenTG()
    {
        Application.OpenURL("https://t.me/+uQFcFVwGmwM3ZDNi");
    }

    public void CheckPromocode()
    {
    	for (int i = 0; i < promocodes.Length; i++)
    	{
    		if (inputField.text == promocodes[i].promocode)
    		{
    			if (Geekplay.Instance.PlayerData.promocodes[i] == 1)
    			{
    				warningText.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0);
    				if (f_cor != null)
    					StopCoroutine(f_cor);
    				f_cor = Fader();
    				StartCoroutine(f_cor);
    				warningText.text = "Этот промокод уже был использован";
    			}
    			else
    			{
    				ShowPanel(i);
    			}
    			break;
    		}
    		if (i == promocodes.Length - 1)
    		{
    			warningText.color = new Color(Color.red.r, Color.red.g, Color.red.b, 0);
    			if (f_cor != null)
    				StopCoroutine(f_cor);
    			f_cor = Fader();
    			StartCoroutine(f_cor);
    			warningText.text = "Такого промокода не существует";
    		}
    	}
    }

    void ShowPanel(int index)
    {
    	rewardPanel.SetActive(true);
    	rewardImage.sprite = promocodes[index].icon;
    	rewardNameText.text = promocodes[index].name;
    	chosen = index;
    }

    public void ClaimReward()
    {
    	promocodes[chosen].TakeReward();
    	Geekplay.Instance.PlayerData.promocodes[chosen] = 1;
    	Geekplay.Instance.Save();
    	rewardPanel.SetActive(false);
    }

    IEnumerator Fader()
    {
    	warningText.DOFade(1, 1);
    	yield return new WaitForSeconds(3);
    	warningText.DOFade(0, 1);	
    }
}
