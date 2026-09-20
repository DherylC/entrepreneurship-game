using UnityEngine;

public class GameplayCameraController : MonoBehaviour
{
    [Header("Default position (set at runtime once the store exists)")]
    public Transform storeTransform;
    // Update: heightAboveStore's default lowered from 15 to 6 - the old value put the camera much
    // farther from a small store than "right beside it" implies. Lower this further if it's still
    // too far, or raise it if the store gets cropped out of frame.
    public float heightAboveStore = 6f;
    public float lookAngle = 35f; // downward tilt in degrees - 30-45 gives the diagonal corner look, not top-down
    // Update: added - the old version never set yaw at all (always 0), which is why the view
    // looked top-down/flat instead of from a diagonal front-right corner. 45 = northeast-ish corner.
    public float yaw = 45f;

    [Header("Terrain clearance - prevents clipping into terrain while panning across uneven ground")]
    public Terrain terrain;
    public float minClearanceAboveTerrain = 2f;

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

        // Update: now includes yaw - previously hardcoded to 0, which is why the view never
        // actually came from a diagonal corner no matter what lookAngle was set to.
        transform.rotation = Quaternion.Euler(lookAngle, yaw, 0f);

        if (storeTransform != null) SnapToStore();
    }

    // Update: full rewrite - the old version computed horizontal offset using the camera's full
    // 3D forward vector (which includes the downward tilt), then overrode Y with a flat world
    // constant afterward. That broke the geometry: once Y is substituted independently, the
    // camera's fixed viewing angle no longer actually lands on the store at all. This version
    // uses trig to solve for the exact horizontal distance that makes the fixed angle correctly
    // look at the store from heightAboveStore - and since height is now relative to the store's
    // own elevation (not an absolute world number), it no longer clips into hilly terrain either.
    public void SnapToStore()
    {
        float cameraY = storeTransform.position.y + heightAboveStore;

        float yawRadians = transform.eulerAngles.y * Mathf.Deg2Rad;
        Vector3 flatForward = new Vector3(Mathf.Sin(yawRadians), 0f, Mathf.Cos(yawRadians));

        float pitchRadians = lookAngle * Mathf.Deg2Rad;
        float horizontalDistance = heightAboveStore / Mathf.Tan(pitchRadians); // exact distance so the view lands on the store

        Vector3 target = storeTransform.position - flatForward * horizontalDistance;
        transform.position = new Vector3(target.x, cameraY, target.z);
    }

    void Update()
    {
        HandleMovement();
        HandlePan();
        HandleZoom();
    }

    // Update: NEW - this is the actual clipping fix. Height was only ever set correctly ONCE,
    // at SnapToStore() time, relative to the store's elevation. But panning/WASD can move the
    // camera across uneven ground while its Y stays exactly where it was - if you roam to a
    // higher patch of terrain, the fixed Y can end up below the actual ground there, clipping
    // into it. This re-samples the terrain height under the camera every frame (after all other
    // movement this frame, via LateUpdate) and pushes the camera up if it's ever too close.
    void LateUpdate()
    {
        if (terrain == null) return;

        float terrainHeightHere = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
        float minAllowedY = terrainHeightHere + minClearanceAboveTerrain;

        if (transform.position.y < minAllowedY)
        {
            Vector3 pos = transform.position;
            pos.y = minAllowedY;
            transform.position = pos;
        }
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