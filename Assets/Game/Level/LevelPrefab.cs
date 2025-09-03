using DG.Tweening;
using UnityEngine;

public class LevelPrefab : MonoBehaviour
{
    [Header("Referencess")]
    public Player player;
    public EnemiesInGame enemiesInGame;
    public SpawnPoints spawnPoints;

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
            foreach (var player in response.players)
            {
                Debug.Log("Init models");
                Debug.Log("player.username - "+player.player_name);
                if (player.player_name == Geekplay.Instance.PlayerData.name)
                {
                    Debug.Log("This is me");
                    var pos = spawnPoints.GetRandomSpawnPoint().position;
                    // Debug.Log(pos + " " + this.player.Controller.transform.position);
                    this.player.Controller.enabled = false;
                    this.player.Controller.transform.position = pos;
                    // this.player.Controller.enabled = true;
                    // Debug.Log(pos + " " + this.player.Controller.transform.position);
                    DOVirtual.DelayedCall(0.1f, () => this.player.Controller.enabled = true);
                }
                else
                {
                    Debug.Log("This is new enemy player");
                    enemiesInGame.InitEnemies(player.heroId, player.playerId);
                }
            }
        });
    }
}