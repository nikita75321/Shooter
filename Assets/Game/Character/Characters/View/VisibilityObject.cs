using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VisibilityObject : MonoBehaviour, IVisible
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Canvas[] canvases;
    [SerializeField] private Rigidbody rb;

    private void OnValidate()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void Awake()
    {
        // Автоматически находим все рендереры если не заданы вручную
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
        if (canvases == null || canvases.Length == 0)
        {
            canvases = GetComponentsInChildren<Canvas>(true);
        }

        foreach (var r in renderers)
        {
            r.enabled = false;
        }
        foreach (var c in canvases)
        {
            c.enabled = false;
        }
    }

    public void SetVisible(bool isVisible)
    {
        // Debug.Log(111);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = isVisible;
        }
        foreach (Canvas c in canvases)
        {
            c.enabled = isVisible;
        }
    }
}