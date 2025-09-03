using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerCustom : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    public Revive revive;
    public GameObject revievButton;
    public Ally ally;
    public TopDownCamera topDownCamera;
    public Joystick joystick;

    [Header("Noize")]
    public float noizeWalk;

    [Header("Stats")]
    public float currentSpeed;
    [SerializeField] private float moveSpeed = 5f;
    public float MoveSpeed
    {
        get => moveSpeed;
        set
        {
            if (value >= 0 && value <= MaxSpeed)
                moveSpeed = value;
        }
    }
    public float MaxSpeed;
    [SerializeField] private float visionRadius;
    public float VisionRadius
    {
        get => visionRadius;
        set
        {
            if (value >= 0)
            {
                visionRadius = value;
                topDownCamera.SetCameraRadiusView(visionRadius);
            }
        }
    }
    public float MaxVisionRadius;
    private Camera cam;
    [Header("Animators")]
    public Animator animator;
    public RuntimeAnimatorController animatorMain;
    public RuntimeAnimatorController animatorSecondary;
    private CharacterController controller;
    [Header("Weapon model")]
    public GameObject secondaryWeaponModel;
    public GameObject mainWeaponModel;

    private float verticalInput;
    private float horizontalInput;
    private float mouseX;
    private float movementBlend;

    private void OnValidate()
    {
        if (animator == null) animator = GetComponent<Animator>();
        // if (joystick == null) joystick = FindAnyObjectByType<JoystickCanvas>().GetJoystick();
        // if (CurrentWeapon == null) CurrentWeapon = GetComponentInChildren<Weapon>();
        // if (Health == null) Health = GetComponent<Health>();
    }

    private void Awake()
    {
        cam = Camera.main;
        controller = GetComponent<CharacterController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Ally>(out var ally))
        {
            revievButton.SetActive(true);
            this.ally = ally;
            revive.ally = ally;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Ally>(out var ally))
        {
            revievButton.SetActive(false);
            this.ally = null;
        }
    }

    private void Start()
    {
        if (Geekplay.Instance.Mobile)
        {
            joystick = JoystickCanvas.Instance.GetJoystick();
        }
    }

    public void Update()
    {
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            GetInput();
            UpdateAnimations();
        }
    }

    private void FixedUpdate()
    {
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            MoveCharacter();
            // WebSocketBase.Instance.SendPlayerTransformUpdate(transform.position, transform.rotation, "ss");
        }
    }
    private void LateUpdate()
    {
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            RotateChar();
        }
    }

    private void GetInput()
    {
        if (!Geekplay.Instance.Mobile)
        {
            verticalInput = Input.GetAxis("Vertical");
            horizontalInput = Input.GetAxis("Horizontal");
            mouseX = Input.GetAxis("Mouse X");
        }
        else
        {
            verticalInput = joystick.Vertical;
            horizontalInput = joystick.Horizontal;
        }
    }

    private Tween movementTween;
    private float movementCheckDelay = 0.2f;
    private void MoveCharacter()
    {
        // Get camera-relative movement direction
        Vector3 moveDirection = cam.transform.up * verticalInput + cam.transform.right * horizontalInput;
        moveDirection.y = 0;

        // Normalize to prevent faster diagonal movement
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        // Apply speed and smooth movement
        currentSpeed = Mathf.Lerp(currentSpeed, moveDirection.magnitude * moveSpeed, 3 * Time.fixedDeltaTime);

        // Move the character
        controller.Move(moveDirection * currentSpeed * Time.fixedDeltaTime);

        // Update movement state with threshold
        player.IsMoving = moveDirection.magnitude > 0.1f;
    }

    private void RotateChar()
    {
        float camYRotation = cam.transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, camYRotation, 0);
    }

    private void UpdateAnimations()
    {
        // Управляем анимацией движения
        animator.SetBool("IsMoving", player.IsMoving);

        // Если нужно использовать триггер (однократное срабатывание)
        if (player.IsMoving && !animator.GetBool("IsMoving"))
        {
            // animator.SetTrigger("WalkForward");
        }
        else if (!player.IsMoving && animator.GetBool("IsMoving"))
        {
            animator.SetTrigger("IsStop");
        }
    }
}