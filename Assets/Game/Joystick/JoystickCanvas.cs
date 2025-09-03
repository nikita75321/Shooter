using UnityEngine;

public class JoystickCanvas : MonoBehaviour
{
    public static JoystickCanvas Instance;

    [Header("References")]
    [SerializeField] private Joystick joystick;

    [Header("Joystick prefab")]
    [SerializeField] private GameObject joystickPrefab;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Geekplay.Instance.Mobile)
        {
            joystickPrefab.SetActive(true);
        }
    }

    private void Update()
    {

    }

    public Joystick GetJoystick()
    {
        return joystick;
    }
}