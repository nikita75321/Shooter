using UnityEngine;

public class DTRotation : MonoBehaviour
{
    [SerializeField] private Vector3 direction;

    private void Update()
    {
        transform.localRotation *= Quaternion.Euler(direction);
    }
}