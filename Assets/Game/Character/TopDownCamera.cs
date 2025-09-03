using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target;  // Цель (персонаж)
    [SerializeField] private Camera cam;       // Камера

    [Header("Settings")]
    [SerializeField] private Vector3 offset;
    [SerializeField] private float height = 10f;          // Высота камеры над персонажем
    [SerializeField] private float rotationSpeed = 5f;    // Скорость вращения
    [SerializeField] private float orbitDistance = 3f;   // Смещение от центра (чтобы камера не была строго по центру)

    [Header("Editor Testing")]
    [SerializeField] private bool simulateTouch = true;
    [SerializeField] private float editorTouchRadius = 100f;

    [Header("Joystick")]
    [SerializeField] private Joystick joystick;
    [SerializeField] private RectTransform joystickArea;
    [SerializeField] private float touchRotationSpeed = 2f;
    [SerializeField] private float deadZoneRadius = 50f;
    private Vector2 touchStartPos;
    private bool isRotatingCamera;
    private bool rightMouseDown;

    private float currentRotationAngle;  // Текущий угол вращения

    private void Start()
    {
        offset = new Vector3(0, 10, orbitDistance);

        if (Geekplay.Instance.Mobile)
        {
            joystick = JoystickCanvas.Instance.GetJoystick();
            joystickArea = joystick.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            if (Geekplay.Instance.Mobile)
            {
                HandleCameraRotationInput();
            }
            else
            {
                // Получаем ввод мыши для вращения
                float mouseX = Input.GetAxis("Mouse X");
                currentRotationAngle += mouseX * rotationSpeed;
            }
        }
    }

    private void HandleCameraRotationInput()
    {
        // Режим редактора - управление правой кнопкой мыши
        if (!Geekplay.Instance.Mobile || simulateTouch)
        {
            // Debug.Log(1);
            if (Input.GetMouseButtonDown(1)) // Правая кнопка мыши
            {
                Debug.Log("нажал");
                rightMouseDown = true;
                touchStartPos = Input.mousePosition;
            }

            if (Input.GetMouseButton(1) && rightMouseDown)
            {
                Vector2 delta = (Vector2)Input.mousePosition - touchStartPos;

                // Имитируем мертвую зону
                if (delta.magnitude > editorTouchRadius * 0.2f)
                {
                    currentRotationAngle += delta.x * rotationSpeed * Time.deltaTime;
                    touchStartPos = Input.mousePosition;
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                rightMouseDown = false;
            }
        }
        else // Режим мобильного устройства
        {
            // Debug.Log(2);
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (!RectTransformUtility.RectangleContainsScreenPoint(joystickArea, touch.position))
                {
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            touchStartPos = touch.position;
                            isRotatingCamera = true;
                            break;

                        case TouchPhase.Moved:
                            if (isRotatingCamera)
                            {
                                currentRotationAngle += touch.deltaPosition.x * touchRotationSpeed * Time.deltaTime;
                            }
                            break;

                        case TouchPhase.Ended:
                            isRotatingCamera = false;
                            break;
                    }
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (GameStateManager.Instance.GameState != GameState.game || target == null) return;

        UpdateCameraPosition();
        // if (GameStateManager.Instance.GameState == GameState.game)
        // {
        //     if (target == null) return;
        //     // 1. Вычисляем позицию камеры по кругу вокруг персонажа
        //     Vector3 orbitOffset = Quaternion.Euler(0, currentRotationAngle, 0) * offset;
        //     Vector3 cameraPosition = target.position + orbitOffset;

        //     // 2. Устанавливаем позицию камеры
        //     transform.position = cameraPosition;

        //     // 3. Направляем камеру строго вниз, но с небольшим смещением вперед для лучшего обзора
        //     transform.rotation = Quaternion.Euler(90f, currentRotationAngle, 0f);
        // }
    }

    private void UpdateCameraPosition()
    {
        // 1. Вычисляем позицию камеры по кругу вокруг персонажа
        Vector3 orbitOffset = Quaternion.Euler(0, currentRotationAngle, 0) * offset;
        Vector3 cameraPosition = target.position + orbitOffset;

        // 2. Устанавливаем позицию камеры
        transform.position = cameraPosition;

        // 3. Направляем камеру строго вниз, но с небольшим смещением вперед
        transform.rotation = Quaternion.Euler(90f, currentRotationAngle, 0f);
    }

    public void SetCameraRadiusView(float value)
    {
        height = value;
        cam.orthographicSize = height;
        orbitDistance++;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (simulateTouch && !Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, editorTouchRadius);
        }
    }
}