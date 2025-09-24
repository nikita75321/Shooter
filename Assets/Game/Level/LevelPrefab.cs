using DG.Tweening;
using UnityEngine;

public class LevelPrefab : MonoBehaviour
{
    [Header("Referencess")]
    public Player player;
    public EnemiesInGame enemiesInGame;
    public SpawnPoints spawnPoints;
    public BotsInGame botsInGame;

    private void OnValidate()
    {
        if (botsInGame == null)
            botsInGame = GetComponentInChildren<BotsInGame>();
    }

    public void Start()
    {
        WebSocketBase.Instance.OnMatchStart += InitEnemy;
    }

    public void OnDestroy()
    {
        WebSocketBase.Instance.OnMatchStart -= InitEnemy;
    }

    private void InitEnemy(MatchStartResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            InstanceSoundUI.Instance.PlayGameBack();

            foreach (var player in response.players)
            {
                Debug.Log("Init models");
                // Debug.Log("player.username - "+player.player_name);
                if (player.player_name == Geekplay.Instance.PlayerData.name)
                {
                    Debug.Log("This is me");
                    var pos = spawnPoints.GetRandomSpawnPoint().position;
                    this.player.Controller.characterController.enabled = false;
                    this.player.Controller.transform.position = pos;
                    // DOVirtual.DelayedCall(0.1f, () => this.player.Controller.enabled = true);
                    this.player.Controller.characterController.enabled = true;
                    // Форсируем обновление
                    this.player.Controller.characterController.Move(Vector3.zero);
                }
                else
                {
                    Debug.Log("This is new enemy player");
                    enemiesInGame.InitEnemies(player);

                    Enemy enemy = EnemiesInGame.Instance.GetEnemy(player.playerId);
                    enemy.Health.MaxHealth = player.max_hp;
                    enemy.Armor.MaxArmor = player.max_armor;
                }
            }

            if (botsInGame != null)
            {
                botsInGame.SpawnBots(response.bots);
            }
        });
    }
}