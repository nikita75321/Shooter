using UnityEngine;

public class FootstepEffect : MonoBehaviour
{
    public ParticleSystem footstepParticles; // Система частиц
    public float minRadius = 0.5f;          // Радиус при ходьбе
    public float maxRadius = 2.0f;          // Радиус при беге
    public float maxSpeed = 5.0f;           // Макс. скорость персонажа

    private CharacterController characterController;
    private float nextStepTime;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (footstepParticles == null)
            footstepParticles = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        if (characterController.isGrounded && characterController.velocity.magnitude > 0.1f)
        {
            // Расчёт интервала между шагами (быстрее скорость → чаще шаги)
            float stepInterval = Mathf.Lerp(0.5f, 0.2f, characterController.velocity.magnitude / maxSpeed);

            if (Time.time > nextStepTime)
            {
                // Настраиваем размер круга по скорости
                float currentRadius = Mathf.Lerp(minRadius, maxRadius, characterController.velocity.magnitude / maxSpeed);
                var shape = footstepParticles.shape;
                shape.radius = currentRadius;

                // Запускаем частицы
                footstepParticles.Play();

                // Обновляем время следующего шага
                nextStepTime = Time.time + stepInterval;
            }
        }
    }
}