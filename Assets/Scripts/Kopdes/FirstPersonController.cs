using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSensitivity = 2f;
    public Camera firstPersonCamera;

    CharacterController controller;
    float verticalLook;

    void Awake() => controller = GetComponent<CharacterController>();

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        controller.Move(transform.TransformDirection(new Vector3(h, 0, v)) * moveSpeed * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        verticalLook = Mathf.Clamp(verticalLook - mouseY, -80f, 80f);
        firstPersonCamera.transform.localEulerAngles = new Vector3(verticalLook, 0, 0);

        if (Input.GetMouseButtonDown(0)) Shoot();
    }

    void Shoot()
    {
        Ray ray = firstPersonCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            Thief thief = hit.collider.GetComponent<Thief>();
            if (thief != null) thief.Caught();
        }
    }
}