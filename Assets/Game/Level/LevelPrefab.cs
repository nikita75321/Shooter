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
                Vector3 spawnPosition = player.position;
                Quaternion spawnRotation = player.rotation;

                bool hasServerPosition = spawnPosition.sqrMagnitude > 0.0001f;

                if (!hasServerPosition && spawnPoints != null)
                {
                    var spawnPoint = spawnPoints.GetRandomSpawnPoint();
                    if (spawnPoint != null)
                    {
                        spawnPosition = spawnPoint.position;
                        spawnRotation = spawnPoint.rotation;
                    }
                }

                player.position = spawnPosition;
                player.rotation = spawnRotation;

                if (player.player_name == Geekplay.Instance.PlayerData.name)
                {
                    Debug.Log("This is me");
                    this.player.Controller.characterController.enabled = false;
                    this.player.Controller.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                    this.player.Controller.characterController.enabled = true;
                    this.player.Controller.characterController.Move(Vector3.zero);
                }
                else
                {
                    Debug.Log("This is new enemy player");
                    enemiesInGame.InitEnemies(player, spawnPosition, spawnRotation);

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