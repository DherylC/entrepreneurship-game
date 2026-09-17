using Unity.VisualScripting;
using UnityEngine;

public class CCTV2 : MonoBehaviour
{
    public float maxAngle = 90f;
    public float rotSpeed = 50.0f;

    GameObject target;
    Vector3 forwardAwal;

    void Start()
    {
        target = GameObject.Find("hero");
        forwardAwal = this.transform.forward;
    }

    void Update()
    {
        Vector3 jarakCctvHero = target.transform.position - this.transform.position;
        float sudut = Vector3.Angle(forwardAwal, jarakCctvHero);

        if (sudut <= maxAngle)
        {
            Quaternion rot = Quaternion.LookRotation(jarakCctvHero);
            this.transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, rot.eulerAngles.y, 0), rotSpeed * Time.deltaTime);
            Debug.DrawRay(this.transform.position, jarakCctvHero, Color.red);
        }
    }
}