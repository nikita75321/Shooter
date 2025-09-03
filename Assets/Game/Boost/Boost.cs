using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public abstract class Boost : MonoBehaviour
{
    [Header("Settings")]
    public float TimeToPickUp = 2f;

    [Header("Visuals")]
    public Animator Animator;
    public Image RadialProgressImage;

    private Coroutine pickUpCoroutine;
    public bool isPickingUp;
    private float currentPickUpTime;
    public Player player;

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
                if (other.GetComponentInParent<Player>() && !isPickingUp && CanPickUp(other.GetComponentInParent<Player>()))
                {
                    Debug.Log("trigger pick up");
                    player = other.GetComponentInParent<Player>();
                    StartPickUp();
                }
                else
                    Debug.Log("trigger pick up NOT WORK");
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
            if (other is CharacterController)
            {
                if (other.GetComponentInParent<Player>() && isPickingUp)
                {
                    CancelPickUp();
                }
            }
        }
    }

    public void StartPickUp()
    {
        if (!CanPickUp(player)) return;

        isPickingUp = true;
        currentPickUpTime = 0f;
        ResetProgress();

        pickUpCoroutine = StartCoroutine(PickUpProcess());

        if (Animator != null)
        {
            Animator.SetBool("IsPickingUp", true);
            Animator.SetFloat("PickUpSpeed", 1f / TimeToPickUp);
        }
    }

    private void CancelPickUp()
    {
        if (pickUpCoroutine != null)
        {
            StopCoroutine(pickUpCoroutine);
        }

        isPickingUp = false;
        ResetProgress();

        if (Animator != null)
        {
            Animator.SetBool("IsPickingUp", false);
        }
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

        if (Animator != null)
        {
            Animator.SetFloat("PickUpProgress", progress);
        }
    }

    public void ResetProgress()
    {
        if (RadialProgressImage != null)
        {
            RadialProgressImage.fillAmount = 0f;
        }

        if (Animator != null)
        {
            Animator.SetFloat("PickUpProgress", 0f);
        }
    }

    protected virtual void CompletePickUp()
    {
        isPickingUp = false;
        ApplyBoostEffect(player);
        gameObject.SetActive(false);
    }
    protected virtual void CompletePickUp(Upgrade upgrade)
    {
        isPickingUp = false;
        player.AddUpgrade(upgrade);
        gameObject.SetActive(false);
    }

    public abstract void ApplyBoostEffect(Player player);
    public abstract bool CanPickUp(Player player);
}