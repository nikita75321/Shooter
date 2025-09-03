
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
// WebSocketBase.Instance.RequestPlayerData("27-89804");
public class Cheat : MonoBehaviour
{
    public static Cheat Instance;
    [SerializeField] private Health health;
    [SerializeField] private Weapon weapon;
    [SerializeField] private Player player;

    private void Awake()
    {
        Instance = this;
    }

    public void Init(Player player)
    {
        this.player = player;
        health = player.Character.Health;
    }
    private void Update()
    {
        CheckCheatSequences();

        if (player != null)
        {
            if (weapon != player.Character.CurrentWeapon)
            {
                weapon = player.Character.CurrentWeapon;
            }
        }
    }

    // Последовательности клавиш для читов
    [Header("Gameplay")]
    [ShowInInspector] private readonly KeyCode[] _hellFireRate = { KeyCode.F, KeyCode.I, KeyCode.R, KeyCode.E, KeyCode.Alpha6, KeyCode.Alpha6, KeyCode.Alpha6 };
    [ShowInInspector] private readonly KeyCode[] _snailFireRate = { KeyCode.F, KeyCode.I, KeyCode.R, KeyCode.E, KeyCode.Alpha0, KeyCode.Alpha0, KeyCode.Alpha0 };
    [ShowInInspector] private readonly KeyCode[] _ammoAdd = { KeyCode.R, KeyCode.UpArrow, KeyCode.UpArrow };
    [ShowInInspector] private readonly KeyCode[] _ammoAddValue = { KeyCode.R, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.UpArrow };
    [ShowInInspector] private readonly KeyCode[] _getDamage = { KeyCode.Z, KeyCode.X, KeyCode.C };
    [ShowInInspector] private readonly KeyCode[] _timeScale01f = { KeyCode.DownArrow, KeyCode.Alpha0, KeyCode.Alpha1 };
    [ShowInInspector] private readonly KeyCode[] _timeScale05f = { KeyCode.DownArrow, KeyCode.Alpha0, KeyCode.Alpha5 };
    [ShowInInspector] private readonly KeyCode[] _timeScaleNormalMotion = { KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.UpArrow };
    [ShowInInspector] private readonly KeyCode[] _timeScaleSpeedUpMotion = { KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.Alpha1 };
    [ShowInInspector] private readonly KeyCode[] _win = { KeyCode.W, KeyCode.I, KeyCode.N };
    [ShowInInspector] private readonly KeyCode[] _lose = { KeyCode.L, KeyCode.O, KeyCode.S, KeyCode.E };

    [Header("Menu")]
    [SerializeField] private Currency currency;
    [ShowInInspector] private readonly KeyCode[] _openAllCharacters = { KeyCode.A,
                                                                        KeyCode.L,
                                                                         KeyCode.L,
                                                                          KeyCode.C, 
                                                                           KeyCode.H, 
                                                                            KeyCode.A, 
                                                                             KeyCode.R };
    [ShowInInspector] private readonly KeyCode[] _saveData = { KeyCode.S, KeyCode.A, KeyCode.V, KeyCode.E };
    [ShowInInspector] private readonly KeyCode[] _addMoney1 = { KeyCode.M,
                                                                KeyCode.O,
                                                                 KeyCode.N,
                                                                  KeyCode.E, 
                                                                   KeyCode.Y, 
                                                                    KeyCode.Alpha1 };
    [ShowInInspector] private readonly KeyCode[] _addMoney2 = { KeyCode.M,
                                                                KeyCode.O,
                                                                 KeyCode.N,
                                                                  KeyCode.E, 
                                                                   KeyCode.Y, 
                                                                    KeyCode.Alpha2 };
    [ShowInInspector] private readonly KeyCode[] _addMoney3 = { KeyCode.M,
                                                                KeyCode.O,
                                                                 KeyCode.N,
                                                                  KeyCode.E, 
                                                                   KeyCode.Y, 
                                                                    KeyCode.Alpha3 };
    [ShowInInspector] private readonly KeyCode[] _addDonatMoney = { KeyCode.D,
                                                                     KeyCode.O,
                                                                      KeyCode.N,
                                                                       KeyCode.A, 
                                                                        KeyCode.T, 
                                                                         KeyCode.Alpha0 };
    [ShowInInspector] private readonly KeyCode[] _addCard1 = { KeyCode.C,
                                                                KeyCode.A,
                                                                 KeyCode.R,
                                                                  KeyCode.D,  
                                                                   KeyCode.Alpha1 };
    [ShowInInspector] private readonly KeyCode[] _addCard2 = { KeyCode.C,
                                                                KeyCode.A,
                                                                 KeyCode.R,
                                                                  KeyCode.D,  
                                                                   KeyCode.Alpha2 };
    [ShowInInspector] private readonly KeyCode[] _addCard3 = { KeyCode.C,
                                                                KeyCode.A,
                                                                 KeyCode.R,
                                                                  KeyCode.D,  
                                                                   KeyCode.Alpha3 };
    [ShowInInspector] private readonly KeyCode[] _load = { KeyCode.L,
                                                            KeyCode.O,
                                                             KeyCode.A,
                                                              KeyCode.D };
    [ShowInInspector] private readonly KeyCode[] _test = { KeyCode.T,
                                                            KeyCode.E,
                                                            KeyCode.S,
                                                            KeyCode.T,
                                                            KeyCode.Alpha1 };
    [ShowInInspector] private readonly KeyCode[] _leaveClan = { KeyCode.C,
                                                                KeyCode.L,
                                                                KeyCode.A,
                                                                KeyCode.N,
                                                                KeyCode.Alpha1 };
    public string loadId;

    private List<KeyCode> _currentInputSequence = new List<KeyCode>();
    private float _sequenceTimeout = 2f; // Время между нажатиями в последовательности
    private float _lastKeyPressTime;
    
    [Header("HellFire Settings")]
    [SerializeField] private float hellFireRate = 0.05f; // Настраиваемое значение
    [SerializeField] private float snailFireRate = 2f;
    [SerializeField] private float hellFireDuration = 20f; // Настраиваемая длительность
    [SerializeField] private float snailFireDuration = 20f; // Настраиваемая длительность

    private void CheckCheatSequences()
    {
        // Проверяем все возможные клавиши для читов
        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    ProcessKeyPress(key);
                    break;
                }
            }
        }

        // Сбрасываем последовательность если прошло слишком много времени
        if (Time.time - _lastKeyPressTime > _sequenceTimeout && _currentInputSequence.Count > 0)
        {
            _currentInputSequence.Clear();
        }
    }

    private void ProcessKeyPress(KeyCode key)
    {
        _lastKeyPressTime = Time.time;
        _currentInputSequence.Add(key);

        #region Gameplay
        if (CheckSequence(_ammoAdd))
        {
            if (player != null && weapon != null)
            {
                player.Character.AddAmmo();
                Debug.Log("(Cheat) Add ammo!");
                return;
            }
        }
        if (CheckSequence(_hellFireRate))
        {
            player.Character.CurrentWeapon.HellFireRate(hellFireRate, hellFireDuration);
            Debug.Log("(Cheat) Hell FireRate! on 10sec");
            return;
        }
        if (CheckSequence(_snailFireRate))
        {
            player.Character.CurrentWeapon.SnailFireRate(snailFireRate, snailFireDuration);
            Debug.Log("(Cheat) Hell FireRate! on 10sec");
            return;
        }
        if (CheckSequence(_ammoAddValue))
        {
            if (player != null && weapon != null)
            {
                player.Character.AddAmmo(15);
                Debug.Log("(Cheat) Add ammo 15!");
                return;
            }
        }
        if (CheckSequence(_timeScale01f))
        {
            Time.timeScale = 0.1f;
            Debug.Log("(Cheat) Slow Motion 0.1f!");
            return;
        }
        if (CheckSequence(_timeScale05f))
        {
            Time.timeScale = 0.5f;
            Debug.Log("(Cheat) Slow Motion 0.5f!");
            return;
        }
        if (CheckSequence(_timeScaleNormalMotion))
        {
            Time.timeScale = 1f;
            Debug.Log("(Cheat) Normal Motion!");
            return;
        }
        if (CheckSequence(_timeScaleSpeedUpMotion))
        {
            Time.timeScale = 10f;
            Debug.Log("(Cheat) SpeedUp 10x Motion!");
            return;
        }
        if (CheckSequence(_getDamage))
        {
            health.TakeDamage(20, 0);
            Debug.Log("(Cheat) Take Damage!");
            return;
        }
        if (CheckSequence(_win))
        {
            player.gameEndCanvas.ShowWinPanel();
            GameStateManager.Instance.matchState = MatchState.win;
            Debug.Log("(Cheat) Win!");
            return;
        }
        if (CheckSequence(_lose))
        {
            player.gameEndCanvas.ShowLosePanel();
            GameStateManager.Instance.matchState = MatchState.lose;
            Debug.Log("(Cheat) Lose!");
            return;
        }
        #endregion
        #region Menu
        if (CheckSequence(_openAllCharacters))
        {
            for (int i = 0; i < Geekplay.Instance.PlayerData.openHeroes.Length; i++)
            {
                Geekplay.Instance.PlayerData.openHeroes[i] = 1;
            }

            Debug.Log("(Cheat) All Characters!");
            return;
        }
        if (CheckSequence(_saveData))
        {
            Geekplay.Instance.Save();
            Debug.Log("(Cheat) Save data!");
            return;
        }
        if (CheckSequence(_addMoney1))
        {
            currency.AddMoney(1000);
            Debug.Log("(Cheat) Add money + 1000!");
            return;
        }
        if (CheckSequence(_addMoney2))
        {
            currency.AddMoney(10000);
            Debug.Log("(Cheat) Add money + 10000!");
            return;
        }
        if (CheckSequence(_addMoney3))
        {
            currency.AddMoney(100000);
            Debug.Log("(Cheat) Add money + 100000!");
            return;
        }
        if (CheckSequence(_addDonatMoney))
        {
            currency.AddDonatMoney(100);
            Debug.Log("(Cheat) Add donat money + 100!");
            return;
        }
        if (CheckSequence(_addCard1))
        {
            HeroCards.Instance.AddCard(5);
            Debug.Log("(Cheat) Add hero card + 5!");
            return;
        }
        if (CheckSequence(_addCard2))
        {
            HeroCards.Instance.AddCard(25);
            Debug.Log("(Cheat) Add hero card + 25!");
            return;
        }
        if (CheckSequence(_addCard3))
        {
            HeroCards.Instance.AddCard(100);
            Debug.Log("(Cheat) Add hero card + 100!");
            return;
        }
        if (CheckSequence(_load))
        {
            WebSocketBase.Instance.RequestPlayerData(loadId);
            Debug.Log($"(Cheat) Load new data {loadId}!");
            return;
        }
        if (CheckSequence(_test))
        {
            WebSocketBase.Instance.LeaveClan();
            Debug.Log($"(Cheat) Test (cur leaveClan) {loadId}!");
            return;
        } 
        if (CheckSequence(_leaveClan))
        {
            Geekplay.Instance.PlayerData.clanId = "";
            Geekplay.Instance.PlayerData.clanName = "";
            Debug.Log($"(Cheat) Test (cur leaveClan) {loadId}!");
            return;
        }
#endregion
    }
    
    private bool CheckSequence(KeyCode[] sequence)
    {
        if (_currentInputSequence.Count < sequence.Length)
            return false;
        
        for (int i = 0; i < sequence.Length; i++)
        {
            if (_currentInputSequence[_currentInputSequence.Count - sequence.Length + i] != sequence[i])
            {
                return false;
            }
        }
        
        return true;
    }
}
