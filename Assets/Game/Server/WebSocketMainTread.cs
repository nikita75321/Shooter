using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class WebSocketMainTread : MonoBehaviour
{
    public static WebSocketMainTread Instance;

    #region Queue actions
    [ShowInInspector] public Queue<Action> mainTreadAction = new();
    private void Update()
    {
        while (mainTreadAction.Count > 0)
        {
            var action = mainTreadAction.Dequeue();
            action?.Invoke();
        }
    }
    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            // Debug.Log(1);
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Debug.Log(2);
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // InitSocket();
        // StartCoroutine(InitWebSocket());
    }
    
}
