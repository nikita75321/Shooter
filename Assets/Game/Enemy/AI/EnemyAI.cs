using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private CharacterController controller;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float circleRadius = 3f;

    [Header("State")]
    public bool isMove;
    [SerializeField] private bool patrol = true;
    [SerializeField] private bool circle = false;

    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private float angle = 0f;
    private bool movingForward = true;
    private Vector3 circleCenter;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        startPosition = transform.position;
        patrolTarget = startPosition + transform.forward * patrolDistance;
        circleCenter = transform.position;

        // Фиксируем вращение по X и Z для топ-даун перспективы
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    private void Update()
    {
        if (enemy.Health.CurrentHealth > 0)
        {
            if (patrol)
            {
                PatrolMovement();
                enemy.UpdateNoizeState(moveSpeed * 2);
                
            }
            else if (circle)
            {
                CircleMovement();
                enemy.UpdateNoizeState(moveSpeed * 2);
            }
            // Debug.Log("Allive");
        }
        // Debug.Log("Die");
    }

    private void PatrolMovement()
    {
        // Рассчитываем направление движения (только по X и Z)
        Vector3 direction = new Vector3(
            patrolTarget.x - transform.position.x,
            0,
            patrolTarget.z - transform.position.z).normalized;

        // Двигаемся с помощью CharacterController
        controller.Move(direction * moveSpeed * Time.deltaTime);

        // Поворачиваем только по оси Y (для топ-даун)
        if (direction != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, targetAngle, 0);
        }

        // Если достигли цели, меняем направление
        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                            new Vector3(patrolTarget.x, 0, patrolTarget.z)) < 0.5f)
        {
            if (movingForward)
            {
                patrolTarget = startPosition - new Vector3(transform.forward.x, 0, transform.forward.z) * patrolDistance;
            }
            else
            {
                patrolTarget = startPosition + new Vector3(transform.forward.x, 0, transform.forward.z) * patrolDistance;
            }
            movingForward = !movingForward;
        }
        isMove = true;
    }

    private void CircleMovement()
    {
        // Увеличиваем угол для движения по кругу
        angle += moveSpeed * Time.deltaTime;

        // Рассчитываем новую позицию на окружности (только X и Z)
        Vector3 newPosition = circleCenter + new Vector3(
            Mathf.Sin(angle) * circleRadius,
            0,
            Mathf.Cos(angle) * circleRadius);

        // Направление движения (горизонтальное)
        Vector3 moveDirection = new Vector3(
            newPosition.x - transform.position.x,
            0,
            newPosition.z - transform.position.z).normalized;

        // Двигаемся
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Поворачиваем только по оси Y
        if (moveDirection != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, targetAngle, 0);
        }
        isMove = true;
    }

    public void SetMovementMode(bool patrolMode, bool circleMode)
    {
        patrol = patrolMode;
        circle = circleMode;

        if (patrol)
        {
            startPosition = new Vector3(transform.position.x, 0, transform.position.z);
            patrolTarget = startPosition + new Vector3(transform.forward.x, 0, transform.forward.z) * patrolDistance;
            movingForward = true;
        }
        else if (circle)
        {
            circleCenter = new Vector3(transform.position.x, 0, transform.position.z);
            angle = Mathf.Atan2(
                transform.position.x - circleCenter.x,
                transform.position.z - circleCenter.z);
        }
    }
}