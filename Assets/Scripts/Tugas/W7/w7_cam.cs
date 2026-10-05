using UnityEngine;

public class w7_cam : MonoBehaviour
{
    private float turnSpeed = 4.0f;
    private Transform posHero;
    private Vector3 offset;

    [Header("Raycast Settings")]
    public float rayDistance = 100f;
    public LayerMask hitLayers;

    void Start()
    {
        posHero = GameObject.Find("hero").transform.Find("char_point_cam").transform;
        offset = new Vector3(posHero.localPosition.x, posHero.localPosition.y, posHero.localPosition.z - 3.2f);

        // Include all layers EXCEPT the "Player" layer
        hitLayers = ~LayerMask.GetMask("Player");
    }

    void Update()
    {
        offset = Quaternion.AngleAxis(Input.GetAxis("Mouse X") * turnSpeed, Vector3.up) * offset;
        transform.position = posHero.position + offset;
        transform.LookAt(posHero.position);

        PerformCameraRaycast();
    }

    private void PerformCameraRaycast()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);

        if (Physics.Raycast(ray, out hit, rayDistance, hitLayers))
        {
            Debug.Log("Camera Raycast hit: " + hit.collider.name);
        }
    }
}