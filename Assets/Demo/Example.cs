using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour
{
    void Awake()
    {
        InitSocket.socketConnected += status =>
        {
            if (status)
            {
                WebSocketBase.Instance.GetStats();
            }
        };
    }
}
