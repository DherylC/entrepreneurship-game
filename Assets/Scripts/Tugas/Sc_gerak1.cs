using UnityEngine;
using UnityEngine.UIElements;
public class Sc_gerak1 : MonoBehaviour
{
    void Update()
    {
        gerakkan5();
    }
    void gerakkan5()
    {
        this.transform.Translate(Vector3.forward * Time.deltaTime, Space.World);
        this.transform.Rotate(Vector3.up * 1f);
    }
}