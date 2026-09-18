using UnityEngine;

public class GameplayCameraController : MonoBehaviour
{
    [Header("Default position (set at runtime once the store exists)")]
    public Transform storeTransform;
    public float fixedHeight = 30f;         // camera's constant Y position - never changes after Start()
    public float lookAngle = 50f;           // downward tilt in degrees (0 = flat horizon, 90 = straight top-down)
    public float defaultDistanceBack = 15f; // horizontal offset behind the store when the scene first loads

    // Update: added WASD movement, since drag-panning alone wasn't enough - matches the
    // request for keyboard movement here too, same as MapPlacement's camera already has.
    [Header("Movement (WASD)")]
    public float moveSpeed = 20f;
    public float fastMoveMultiplier = 2f; // hold Left Shift to move faster

    // Update: since this camera's height is permanently fixed (no altitude to scale off, unlike
    // MapCameraController), "more zoomed out = faster" is driven by orthographic size instead -
    // a bigger orthographic size means you're seeing more of the map at once, i.e. zoomed out.
    [Header("Zoom-based speed scaling - more zoomed OUT (larger orthographic size) = faster")]
    public float referenceOrthoSize = 10f; // moveSpeed applies as-written at this zoom level
    public float minZoomSpeedMultiplier = 0.3f;
    public float maxZoomSpeedMultiplier = 5f;

    [Header("Pan (hold left OR right mouse button + drag)")]
    public float panSpeed = 1f;

    [Header("Zoom (scroll wheel - adjusts orthographic size, camera position never moves)")]
    public Camera cam;
    public float zoomSpeed = 5f;
    public float minOrthoSize = 5f;
    public float maxOrthoSize = 40f;

    Vector3 lastMousePosition;

    void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
        cam.orthographic = true; // required for fixed-height zoom to make sense

        transform.rotation = Quaternion.Euler(lookAngle, 0f, 0f);

        if (storeTransform != null) SnapToStore();
    }

    // Called by GameplaySceneBootstrapper right after the store is instantiated.
    public void SnapToStore()
    {
        Vector3 backOffset = -transform.forward * defaultDistanceBack;
        Vector3 target = storeTransform.position + backOffset;
        transform.position = new Vector3(target.x, fixedHeight, target.z); // Y is always fixedHeight, never anything else
    }

    void Update()
    {
        HandleMovement();
        HandlePan();
        HandleZoom();
    }

    float CurrentZoomSpeedMultiplier() =>
        Mathf.Clamp(cam.orthographicSize / referenceOrthoSize, minZoomSpeedMultiplier, maxZoomSpeedMultiplier);

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S
        float speed = moveSpeed * CurrentZoomSpeedMultiplier() * (Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f);

        Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
        Vector3 right = transform.right; right.y = 0; right.Normalize();

        Vector3 move = (forward * v + right * h) * speed * Time.deltaTime;
        move.y = 0f; // height stays permanently fixed, same rule as everywhere else in this camera
        transform.position += move;
    }

    void HandlePan()
    {
        // Update: MainGameplay doesn't need left-click reserved for anything exclusive
        // (unlike MapPlacement, where left-click must stay dedicated to confirming placement),
        // so both buttons work here for a more forgiving feel.
        bool panPressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
        bool panHeld = Input.GetMouseButton(0) || Input.GetMouseButton(1);

        if (panPressed) lastMousePosition = Input.mousePosition;

        if (panHeld)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 right = transform.right;
            Vector3 forwardFlat = Vector3.Cross(right, Vector3.up); // flat forward, ignoring the downward tilt

            Vector3 move = (-right * delta.x - forwardFlat * delta.y) * panSpeed * Time.deltaTime;
            move.y = 0f; // height is never touched by panning

            transform.position += move;
            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minOrthoSize, maxOrthoSize);
    }
}