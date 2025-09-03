using UnityEngine;
using UnityEngine.UI;

public class HeroSkinLock : MonoBehaviour
{
    [SerializeField] private Button buttonLock;

    private void OnValidate()
    {
        if (buttonLock == null) buttonLock = GetComponent<Button>();
    }

    private void HideLock()
    {

    }

    private void ShowLock()
    {
        
    }
}
