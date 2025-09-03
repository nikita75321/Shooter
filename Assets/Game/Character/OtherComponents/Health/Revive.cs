using DG.Tweening;
using UnityEngine;

public class Revive : MonoBehaviour
{
    public Player player;
    public Ally ally;
    [SerializeField] private float timeToRevive = 5;
    private Tween reviveTween; // Сохраняем ссылку на tween для возможности прерывания

    private void OnValidate()
    {
        if (player == null) player = FindAnyObjectByType<Player>(FindObjectsInactive.Include);
    }

    public void StartRevive()
    {
        // Прерываем предыдущий tween, если он был
        reviveTween?.Kill();
        
        reviveTween = DOVirtual.Float(0, 1, timeToRevive, (value) =>
        {
            player.Character.Health.useReviveImage.fillAmount = value;
        }).OnComplete(() =>
        {
            CompleteRevive();
        }).OnKill(() => 
        {
            // Сбрасываем заполнение при прерывании
            player.Character.Health.useReviveImage.fillAmount = 0;
        });
    }

    public void StopRevive()
    {
        // Прерываем tween
        reviveTween?.Kill();
        reviveTween = null;
        
        // Сбрасываем состояние
        player.Character.Health.useReviveImage.fillAmount = 0;
        player.IsRevive = false;
    }

    private void CompleteRevive()
    {
        player.Character.Health.useReviveImage.fillAmount = 0;
        player.Controller.revievButton.SetActive(false);
        ally.health.Revive();

        ally.animator.SetTrigger("Revive");
        ally.animator.SetLayerWeight(1, 1);
        ally.animator.SetLayerWeight(2, 1);

        ally.healthbar.gameObject.SetActive(true);
        ally.armorBar.gameObject.SetActive(true);
        ally.reviveCollider.enabled = false;
        ally.controller.enabled = true;
        player.IsRevive = false;

        Geekplay.Instance.PlayerData.reviveAlly++;
        Geekplay.Instance.Save();
    }
}