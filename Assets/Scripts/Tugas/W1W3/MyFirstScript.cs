using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MyFirstScript : MonoBehaviour {
    int awake = 0, start = 0, update = 0, fixedUpdate = 0, lateUpdate = 0;

    void Awake()
    {
        awake++;
        Debug.Log("Awake yang ke-" + awake);
    }
    void Start()
    {
        start++;
        Debug.Log("Start yang ke-" + start);
    }
    void Update()
    {
        update++;
        Debug.Log("Update yang ke-" + update + "pada: " + Time.deltaTime);
    }
    void FixedUpdate()
    {
        fixedUpdate++;
        Debug.Log("FixedUpdate yang ke-" + fixedUpdate + "pada: " + Time.deltaTime);
    }
    void LateUpdate()
    {
        lateUpdate++;
        Debug.Log("LateUpdate yang ke-" + lateUpdate + "pada: " + Time.deltaTime);
    }
}
