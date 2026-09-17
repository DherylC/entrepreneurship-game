using UnityEngine;


public class Hero : MonoBehaviour
{

    void Start()
    {
        this.transform.rotation = Quaternion.Euler(new Vector3(0, 30, 0));
        Debug.Log("Rotasi y Hero: " + this.transform.eulerAngles.y);
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        this.transform.Translate(new Vector3(0, 0, v) * Time.deltaTime * 3f);
        this.transform.Rotate(new Vector3(0, h, 0) * 5f);
    }
}
