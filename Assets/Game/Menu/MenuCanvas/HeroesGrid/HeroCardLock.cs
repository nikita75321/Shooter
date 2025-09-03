using UnityEngine;

public class HeroCardLock : MonoBehaviour
{
    public void ShowLock()
    {
        gameObject.SetActive(true);
    }
    public void HideLock()
    {
        gameObject.SetActive(false);
    }
}
