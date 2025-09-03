using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadCanvas : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Level level;
    [SerializeField] private GameObject[] turnOn;
    [SerializeField] private GameObject[] turnOff;

    [Header("Time to load")]
    [SerializeField] private int timeToLoad;

    [Header("Hints")]
    [SerializeField] private Image hintImage;
    [SerializeField] private Sprite[] hints;
    [SerializeField] private string[] hintsText;
    [SerializeField] private TMP_Text hintTXT;
    [SerializeField] private Button prevButton, nextButton;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text roomInfoText;
    [SerializeField] private GameObject matchmakingPanel;
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private Button cancelMatchmakingButton;

    private int currentHintIndex = 0;
    private Tween autoChangeTween;
    private bool isInMatchmaking = false;
    private string currentRoomId = "";

    private void OnEnable()
    {
        // Подписываемся на события WebSocket
        WebSocketBase.Instance.OnMatchmakingJoined += HandleMatchmakingJoined;
        WebSocketBase.Instance.OnMatchStart += HandleMatchStart;
        WebSocketBase.Instance.OnPlayerLeftRoom += HandlePlayerLeftRoom;
        WebSocketBase.Instance.OnRoomForceClosed += HandleRoomForceClosed;
        WebSocketBase.Instance.OnMatchmakingFull += HandleMatchmakingFull;

        GameStateManager.Instance.GamePause();
        level.InitLevel();
        
        // Инициализация кнопок
        prevButton.onClick.AddListener(ShowPreviousHint);
        nextButton.onClick.AddListener(ShowNextHint);
        // cancelMatchmakingButton.onClick.AddListener(CancelMatchmaking);
        
        // Показываем первую подсказку
        ShowHint(0);
        
        // Запускаем автоматическое переключение
        StartAutoHintChange();

        // Начинаем поиск матча
        StartMatchmaking();
    }

    private void OnDisable()
    {
        // Отписываемся от событий
        WebSocketBase.Instance.OnMatchmakingJoined -= HandleMatchmakingJoined;
        WebSocketBase.Instance.OnMatchStart -= HandleMatchStart;
        WebSocketBase.Instance.OnPlayerLeftRoom -= HandlePlayerLeftRoom;
        WebSocketBase.Instance.OnRoomForceClosed -= HandleRoomForceClosed;
        WebSocketBase.Instance.OnMatchmakingFull -= HandleMatchmakingFull;
        
        // Очищаем подписки на кнопки
        prevButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        // cancelMatchmakingButton.onClick.RemoveAllListeners();

        // Останавливаем автоматическое переключение
        StopAutoHintChange();

        // Если все еще в матчмейкинге, отменяем
        if (isInMatchmaking)
        {
            CancelMatchmaking();
        }
    }

    private void StartMatchmaking()
    {
        isInMatchmaking = true;
        UpdateStatusText("Поиск матча...");
        ShowMatchmakingUI();
        
        // Отправляем запрос на присоединение к матчмейкингу
        WebSocketBase.Instance.JoinMatchmaking(Geekplay.Instance.PlayerData.currentMode);
    }

    private void HandleMatchmakingJoined(RoomJoinedResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            isInMatchmaking = false;
            currentRoomId = response.room_id;

            UpdateStatusText($"Комната найдена! Игроков: {response.players_in_room}/{response.max_players}");
            UpdateRoomInfo($"Комната: {response.room_id}\nОжидание игроков...");
            ShowRoomUI();

            Debug.Log($"Joined room: {response.room_id}, Players: {response.players_in_room}/{response.max_players}");
        });
    }

    private void HandleMatchStart(MatchStartResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            Debug.Log($"Received match_start for room: {response.room_id}, Current room: {currentRoomId}");

            if (response.room_id == currentRoomId)
            {
                UpdateStatusText("Матч начинается!");
                UpdateRoomInfo($"Матч начался!\nID: {response.match_id}\nИгроков: {response.players.Count}\nБотов: {response.bots.Count}");

                // Сохраняем информацию о комнате
                Geekplay.Instance.PlayerData.roomId = response.room_id;

                // Запускаем загрузку уровня через несколько секунд
                LoadLevel();
                // DOVirtual.DelayedCall(timeToLoad, LoadLevel);
            }
        });
    }

    private void HandlePlayerLeftRoom(PlayerLeftRoomResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (response.room_id == currentRoomId)
            {
                UpdateRoomInfo($"Игрок вышел. Осталось: {response.players_remaining}");
            }
        });
    }

    private void HandleRoomForceClosed(RoomForceClosedResponse response)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            if (response.room_id == currentRoomId)
            {
                UpdateStatusText("Комната закрыта");
                UpdateRoomInfo("Комната была закрыта администратором");
                
                // Возвращаемся к поиску матча
                DOVirtual.DelayedCall(2f, StartMatchmaking);
            }
        });
    }

    private void HandleMatchmakingFull(string message)
    {
        WebSocketMainTread.Instance.mainTreadAction.Enqueue(() =>
        {
            UpdateStatusText("Все комнаты заполнены");
            UpdateRoomInfo("Попробуйте позже");
            
            // Повторяем поиск через 5 секунд
            DOVirtual.DelayedCall(5f, StartMatchmaking);
        });
    }

    private void CancelMatchmaking()
    {
        
        if (isInMatchmaking)
        {
            // Отправляем запрос на выход из матчмейкинга
            WebSocketBase.Instance.LeaveRoom();
            isInMatchmaking = false;
        }
        
        if (!string.IsNullOrEmpty(currentRoomId))
        {
            // Выходим из комнаты
            WebSocketBase.Instance.LeaveRoom();
            currentRoomId = "";
        }
        
        UpdateStatusText("Поиск отменен");
        ShowMatchmakingUI();
        
        // Возвращаемся в меню или делаем другие действия
        Debug.Log("Matchmaking cancelled");
        
    }

    public void LoadLevel()
    {
        foreach (var off in turnOff)
        {
            off.SetActive(false);
        }
        foreach (var on in turnOn)
        {
            on.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        GameStateManager.Instance.GameStart();
    }

    private void ShowHint(int index)
    {
        if (hints.Length == 0) return;
        
        currentHintIndex = (index + hints.Length) % hints.Length;
        
        hintImage.sprite = hints[currentHintIndex];
        
        if (hintTXT != null && hintsText.Length > currentHintIndex)
        {
            hintTXT.text = hintsText[currentHintIndex];
        }
    }

    public void ShowNextHint()
    {
        var index = currentHintIndex + 1;
        if (index >= hints.Length) index = 0;
        ShowHint(index);
        RestartAutoHintChange();
    }

    public void ShowPreviousHint()
    {
        var index = currentHintIndex - 1;
        if (index < 0) index = hints.Length - 1;
        ShowHint(index);
        RestartAutoHintChange();
    }

    private void StartAutoHintChange()
    {
        StopAutoHintChange();
        
        autoChangeTween = DOVirtual.DelayedCall(3f, () =>
        {
            ShowNextHint();
            StartAutoHintChange();
        }).SetLoops(-1);
    }

    private void StopAutoHintChange()
    {
        if (autoChangeTween != null && autoChangeTween.IsActive())
        {
            autoChangeTween.Kill();
        }
    }

    private void RestartAutoHintChange()
    {
        StopAutoHintChange();
        StartAutoHintChange();
    }

    private void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    private void UpdateRoomInfo(string text)
    {
        if (roomInfoText != null)
        {
            roomInfoText.text = text;
        }
    }

    private void ShowMatchmakingUI()
    {
        if (matchmakingPanel != null) matchmakingPanel.SetActive(true);
        if (roomPanel != null) roomPanel.SetActive(false);
        // if (cancelMatchmakingButton != null) cancelMatchmakingButton.gameObject.SetActive(true);
    }

    private void ShowRoomUI()
    {
        if (matchmakingPanel != null) matchmakingPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(true);
        // if (cancelMatchmakingButton != null) cancelMatchmakingButton.gameObject.SetActive(false);
    }

    // Метод для принудительной загрузки уровня (для тестирования)
    public void ForceLoadLevel()
    {
        LoadLevel();
    }
}