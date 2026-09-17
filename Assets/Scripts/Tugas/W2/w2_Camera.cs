using Unity.VisualScripting;
using UnityEngine;

public class w2_Camera : MonoBehaviour
{
    GameObject hero;
    void Start()
    {
        hero = GameObject.Find("hero");
    }

    void Update()
    {
        float speed = 0f;
        float zoom = 0f;
        if (Input.GetKey(KeyCode.LeftShift)) speed = 50;
        if (Input.GetKey(KeyCode.RightShift)) speed = -50;

        if(Input.GetKey(KeyCode.I)) zoom = 10;
        if (Input.GetKey(KeyCode.O)) zoom = -10;

        this.transform.LookAt(hero.transform);
        transform.Translate(Vector3.forward * zoom * Time.deltaTime);
        this.transform.RotateAround(hero.transform.position, Vector3.up, speed * Time.deltaTime);   
    }
}