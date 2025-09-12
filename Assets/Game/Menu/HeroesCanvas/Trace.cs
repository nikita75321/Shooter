using UnityEngine;

public class Trace : MonoBehaviour
{
    [SerializeField] private Renderer[] traces;
    [SerializeField] private GameObject[] tracesGo;
    [SerializeField] private GameObject[] tracesTarget;

    private void Start()
    {

    }

    private Vector3 offsetTraceY = new(0, 0.1f, 0);
    public void UpdateTracesPos()
    {
        // твой код с traces
        for (int i = 0; i < tracesGo.Length; i++)
        {
            tracesGo[i].transform.position = tracesTarget[i].transform.position + offsetTraceY;
        }
    }
    
    public void ShowTraces()
    {
        foreach (var trace in traces)
        {
            trace.enabled = true;
        }
        Debug.Log("Show trace");
    }
    public void HideTraces()
    {
        foreach (var trace in traces)
        {
            trace.enabled = false;
        }
        Debug.Log("Hide trace");
    }
}