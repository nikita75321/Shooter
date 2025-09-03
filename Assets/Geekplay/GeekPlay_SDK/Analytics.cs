using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameAnalyticsSDK;

public class Analytics : MonoBehaviour
{
    public static Analytics instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;

            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this.gameObject);
        }
       
    }

    void Start()
    {
        GameAnalytics.Initialize();
        SendEvent("Start");
    }

    public void SendEvent(string eventStr)
    {
        try
        {
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, eventStr);
        }
        catch (Exception e)
        {

        }
    }
}