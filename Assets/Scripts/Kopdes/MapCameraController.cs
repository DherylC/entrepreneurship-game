using UnityEngine;

public class MapCameraController : MonoBehaviour
{
    [Header("Movement (WASD)")]
    public float moveSpeed = 20f;
    public float fastMoveMultiplier = 2f; // hold Left Shift to move faster

    [Header("Height-based speed scaling")]
    public float referenceHeight = 20f;          // moveSpeed applies as-is at this height
    public float minHeightSpeedMultiplier = 0.3f; // near the ground, movement is slower/more precise
    public float maxHeightSpeedMultiplier = 5f;    // high up, movement is much faster so it never feels sluggish

    [Header("Look (hold right mouse button + drag)")]
    public float lookSensitivity = 3f;
    public float minPitch = -80f;
    public float maxPitch = 80f;
    float yaw;
    float pitch;

    [Header("Zoom (scroll wheel)")]
    public float zoomSpeed = 10f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleZoom();
    }

    // Update: safety net for a known Unity quirk - if focus is lost while right-click is still
    // held (e.g. alt-tabbing mid-drag), Unity can miss the "button up" event, leaving the cursor
    // stuck locked/hidden. This forces it back to normal the moment focus returns, regardless.
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) Cursor.lockState = CursorLockMode.None;
    }

    // Update: movement speed now scales with the camera's current altitude - flying high up
    // feels much faster, hovering near the ground feels slower/more precise for fine placement.
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        float heightMultiplier = Mathf.Clamp(transform.position.y / referenceHeight, minHeightSpeedMultiplier, maxHeightSpeedMultiplier);
        float speed = moveSpeed * heightMultiplier * (Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f);

        // Move relative to the camera's own flat forward/right (ignoring pitch), so WASD
        // always moves along the ground plane even while looking up or down.
        Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
        Vector3 right = transform.right; right.y = 0; right.Normalize();

        transform.position += (forward * v + right * h) * speed * Time.deltaTime;
    }

    void HandleLook()
    {
        if (Input.GetMouseButtonDown(1)) Cursor.lockState = CursorLockMode.Locked; // hide/lock cursor while dragging
        if (Input.GetMouseButtonUp(1)) Cursor.lockState = CursorLockMode.None;

        if (Input.GetMouseButton(1)) // right mouse button held
        {
            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            transform.position += transform.forward * scroll * zoomSpeed;
    }
}