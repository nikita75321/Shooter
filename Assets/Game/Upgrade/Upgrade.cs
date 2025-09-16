using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class Upgrade : MonoBehaviour
{
    [Header("Info")]
    public int id;
    public bool isPickingUp;
    public UpgradeType type;

    [Header("Prefab")]
    [SerializeField] private GameObject upgradePrefab;

    [Header("Settings")]
    public float TimeToPickUp = 2f;

    [Header("Visuals")]
    // public Animator Animator;
    public Image RadialProgressImage;

    private Coroutine pickUpCoroutine;
    private float currentPickUpTime;
    protected Player player;

    private void Start()
    {
        ResetProgress();
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            if (other is CharacterController)
            {
                player = other.GetComponentInParent<Player>();
                if (player != null && !isPickingUp && CanPickUp(player))
                {
                    StartPickUp();
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            if (other is CharacterController)
            {
                if (isPickingUp && player != null && !CanPickUp(player))
                {
                    CancelPickUp();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (GameStateManager.Instance.GameState == GameState.game)
        {
            if (other.GetComponentInParent<Player>() && isPickingUp)
            {
                CancelPickUp();
            }
        }
    }

    public void StartPickUp()
    {
        if (player == null || !CanPickUp(player)) return;

        isPickingUp = true;
        currentPickUpTime = 0f;
        ResetProgress();

        pickUpCoroutine = StartCoroutine(PickUpProcess());

        // if (Animator != null)
        // {
        //     Animator.SetBool("IsPickingUp", true);
        //     Animator.SetFloat("PickUpSpeed", 1f / TimeToPickUp);
        // }
    }

    private void CancelPickUp()
    {
        if (pickUpCoroutine != null)
        {
            StopCoroutine(pickUpCoroutine);
        }

        isPickingUp = false;
        ResetProgress();

        // if (Animator != null)
        // {
        //     Animator.SetBool("IsPickingUp", false);
        // }
    }

    private IEnumerator PickUpProcess()
    {
        while (currentPickUpTime < TimeToPickUp && CanPickUp(player))
        {
            currentPickUpTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentPickUpTime / TimeToPickUp);
            UpdateProgressVisuals(progress);
            yield return null;
        }

        if (CanPickUp(player))
        {
            CompletePickUp();
        }
        else
        {
            CancelPickUp();
        }
    }

    private void UpdateProgressVisuals(float progress)
    {
        if (RadialProgressImage != null)
        {
            RadialProgressImage.fillAmount = progress;
        }

        // if (Animator != null)
        // {
        //     Animator.SetFloat("PickUpProgress", progress);
        // }
    }

    public void ResetProgress()
    {
        if (RadialProgressImage != null)
        {
            RadialProgressImage.fillAmount = 0f;
        }

        // if (Animator != null)
        // {
        //     Animator.SetFloat("PickUpProgress", 0f);
        // }
    }

    protected virtual void CompletePickUp()
    {
        isPickingUp = false;
        if (player != null)
        {
            player.AddUpgrade(this);
        }
        gameObject.SetActive(false);
    }

    public abstract void ApplyBoostEffect(Player player);
    public abstract void RemoveBoostEffect(Player player);
    public abstract bool CanPickUp(Player player);

    public bool HaveUpgrade(Player player)
    {
        if (player == null) return false;
        return player.upgrades.ContainsKey(GetType());
    }
    public void DropUpgrade(Vector3 playerPosition)
    {
        if (upgradePrefab != null)
        {
            // Параметры разброса

            float dropRadius = 3f; // Радиус разброса
            // float dropHeight = 1f; // Высота появления

            // Случайный угол и расстояние
            float randomAngle = Random.Range(0, 360f);
            float randomDistance = Random.Range(1.5f, dropRadius);

            // Вычисляем позицию
            Vector3 dropPosition = playerPosition +
                                  Quaternion.Euler(0, randomAngle, 0) * Vector3.forward * randomDistance;
            // dropPosition.y += dropHeight;

            // Создаем апгрейд
            ResetProgress();
            UpgradesManager.Instance.DropUpgrade(dropPosition, id);
            // GameObject droppedUpgrade = Instantiate(upgradePrefab, dropPosition, Quaternion.identity);
            // droppedUpgrade.SetActive(true);
        }
    }
}