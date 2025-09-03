using UnityEditor;
using UnityEngine;

public static class MobileState
{
    private const string ToggleMobileMenuStr = "GeekplaySDK/Toggle Mobile State &m";
    private const string MobileStateKey = "Geekplay_MobileState";

    [MenuItem(ToggleMobileMenuStr, false, 151)]
    private static void ToggleMobileStateMenu()
    {
        bool newState = !IsMobile();
        SetMobileState(newState);
        Menu.SetChecked(ToggleMobileMenuStr, newState);
        ShowNotification(newState ? "Mobile state: ON" : "Mobile state: OFF");
    }

    [MenuItem(ToggleMobileMenuStr, true)]
    private static bool ToggleMobileStateMenuValidate()
    {
        Menu.SetChecked(ToggleMobileMenuStr, IsMobile());
        return true;
    }

    public static void SetMobileState(bool isMobile)
    {
        EditorPrefs.SetBool(MobileStateKey, isMobile);
        
        // Если Starter есть в текущей сцене — обновляем его поле
        var starter = Object.FindObjectOfType<Starter>();
        if (starter != null)
        {
            starter.mobile = isMobile;
            Debug.Log($"[MobileState] Updated current scene Starter.mobile to: {isMobile}");
        }
        else
        {
            Debug.Log($"[MobileState] Starter not found in current scene. State saved: {isMobile}");
        }
    }

    public static bool IsMobile()
    {
        return EditorPrefs.HasKey(MobileStateKey) && EditorPrefs.GetBool(MobileStateKey);
    }

    private static void ShowNotification(string msg)
    {
        if (Resources.FindObjectsOfTypeAll<SceneView>().Length > 0)
            EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent(msg));
        else
            Debug.Log(msg);
    }
}