using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Box_Instantiate : MonoBehaviour
{
    public GameObject objPrefab;
    // Use this for initialization
    void Start()
    {
    }
    // Update is called once per frame
    void Update()
    {
        Instantiate(objPrefab, this.transform.position, this.transform.rotation);
    }
}