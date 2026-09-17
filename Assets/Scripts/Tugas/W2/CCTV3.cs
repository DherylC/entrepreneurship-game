using Unity.VisualScripting;
using UnityEngine;


public class CCTV3 : MonoBehaviour
{
    GameObject target;

    void Start()
    {
        target = GameObject.Find("hero");
        this.transform.rotation = Quaternion.AngleAxis(45, Vector3.up);
    }

    void Update()
    {
        Quaternion target = Quaternion.Euler(0, 180, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f * Time.deltaTime);
    }
}
