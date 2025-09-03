using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class UtilsVK : MonoBehaviour
{
    [DllImport("__Internal")]
    public static extern void GamePlatform();

    [DllImport("__Internal")]
    public static extern void VK_Star();

    [DllImport("__Internal")]
    public static extern void VK_Share();

    [DllImport("__Internal")]
    public static extern void VK_Invite();

    [DllImport("__Internal")]
    public static extern void VK_Banner();

    [DllImport("__Internal")]
    public static extern void VK_AdInterCheck();

    [DllImport("__Internal")]
    public static extern void VK_AdRewardCheck();

    [DllImport("__Internal")]
    public static extern void VK_Interstitial();

    [DllImport("__Internal")]
    public static extern void VK_Rewarded();

    [DllImport("__Internal")]
    public static extern void VK_OpenLeaderboard();

    [DllImport("__Internal")]
    public static extern void VK_Load();

    [DllImport("__Internal")]
    public static extern void VK_Save(string saveString);

    [DllImport("__Internal")]
    public static extern void VK_ToGroup();

    [DllImport("__Internal")]
    public static extern void VK_RealBuy(string id);
}
