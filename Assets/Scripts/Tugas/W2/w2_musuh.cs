using System.Collections.Generic;
using UnityEngine;

public class w2_musuh : MonoBehaviour
{
    GameObject movepoint;
    List<GameObject> listPoint = new List<GameObject>();
    float speedMove = 5f;

    void Awake()
    {
        movepoint = GameObject.Find("movepoint");

        foreach (Transform child in movepoint.transform)
        {
            listPoint.Add(child.gameObject);
        }

        foreach (GameObject x in listPoint)
        {
            Debug.Log("List: " + x.ToString());
        }
    }

    void Update()
    {
        if (listPoint.Count > 0)
        {
            Vector3 targetPos = new Vector3(listPoint[0].transform.position.x, this.transform.position.y, listPoint[0].transform.position.z);
            this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, speedMove * Time.deltaTime);

            Vector3 posPoint = targetPos - this.transform.position;
            if (posPoint != Vector3.zero)
            {
                this.transform.rotation = Quaternion.LookRotation(posPoint);
            }

            Vector3 posMusuh = this.transform.position;
            posMusuh.y = listPoint[0].transform.position.y;
            if (Vector3.Distance(listPoint[0].transform.position, posMusuh) < 0.1f)
            {
                listPoint.RemoveAt(0);
            }
        }
        else
        {
            Debug.Log("musuh sampai tujuan");
            Destroy(this.gameObject);
        }
    }
}