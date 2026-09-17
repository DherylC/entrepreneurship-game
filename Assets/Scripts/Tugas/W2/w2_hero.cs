using Mono.Cecil;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class w2_hero : MonoBehaviour
{
    List<GameObject> targetList = new List<GameObject>();
    GameObject target;
    int noList = 0;

    void Update()
    {
        if (target != null) {
            Vector3 targetPos = target.transform.position - this.transform.position;
            this.transform.rotation = Quaternion.LookRotation(targetPos);       
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            noList++;
            if (noList >= targetList.Count) {
                noList = 0;
            }
            pindahTarget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "musuh")
        {
            targetList.Add(other.gameObject);
            pindahTarget();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "musuh")
        {
            targetList.Remove(other.gameObject);

            if (noList >= targetList.Count) {
                noList = 0;
            }

            pindahTarget();
        }
    }

    private void pindahTarget()
    {
        if (targetList.Count > 0) 
        {
            target = targetList[noList];
        }
        foreach (var item in targetList) {
            Debug.Log(item.ToString());
        }
    }

}
