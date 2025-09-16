using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnPoints : MonoBehaviour
{
    public static SpawnPoints Instance;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private List<Transform> tempPoints;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        tempPoints = spawnPoints.ToList();
    }

    private void Update()
    {

    }

    public Transform GetRandomSpawnPoint()
    {
        Debug.Log("Random point get");
        if (tempPoints.Count > 0)
        {
            var t = tempPoints[Random.Range(0, tempPoints.Count)];
            tempPoints.Remove(t);
            return t;
        }
        return null;
    }
}
