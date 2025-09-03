using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Transform cam;
    // Start is called before the first frame update
    void Start()
    {
        transform.LookAt(cam);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
