using System;
using UnityEngine;

public enum GameState
{
    game,
    pause,
    death
}
public enum MatchState
{
    ready,
    lose,
    win
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public GameState GameState;
    public Action GamePause;
    public Action GameStart;
    public Action GameDeath;

    public MatchState matchState;
    
    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameStart += StateStart;
        GamePause += StatePause;
        GameDeath += StateDeath;
    }

    private void OnDisable()
    {
        GameStart -= StateStart;
        GamePause -= StatePause;
        GameDeath -= StateDeath;
    }

    private void StateStart()
    {
        GameState = GameState.game;
        Debug.Log("game-state start");
    }

    private void StatePause()
    {
        GameState = GameState.pause;
        Debug.Log("game-state pause");
    }

    private void StateDeath()
    {
        GameState = GameState.death;
        Debug.Log("game-state death");
    }
}