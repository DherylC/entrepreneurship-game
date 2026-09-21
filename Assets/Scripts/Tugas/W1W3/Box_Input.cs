using UnityEngine;
using System.Collections;
public class Box_Input : MonoBehaviour
{
    float speed = 2f;
    // Use this for initialization
    void Start()
    {
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            this.transform.Translate(new Vector3(-1, 0, 0) * Time.deltaTime * speed);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            this.transform.Translate(new Vector3(1, 0, 0) * Time.deltaTime * speed);
        }
        if (Input.GetKey(KeyCode.W))
        {
            this.transform.Translate(new Vector3(0, 0, 1) * Time.deltaTime * speed);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            this.transform.Translate(new Vector3(0, 0, -1) * Time.deltaTime * speed);
        }
    }
}
