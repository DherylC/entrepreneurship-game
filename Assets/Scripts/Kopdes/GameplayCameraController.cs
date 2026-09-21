using UnityEngine;

public class GameplayCameraController : MonoBehaviour
{
    [Header("Default position (set at runtime once the store exists)")]
    public Transform storeTransform;
    // Update: these are now the CLOSEST-zoom values (zoomLevel = 0). Farthest-zoom values are
    // the new maxHeightAboveStore/maxLookAngle fields below - scrolling interpolates between them.
    public float heightAboveStore = 6f;
    public float lookAngle = 35f; // downward tilt in degrees - 30-45 gives the diagonal corner look, not top-down
    public float yaw = 45f; // fixed forever - 45 = northeast-ish corner

    // Update: NEW - replaces orthographic-size zoom entirely. Zooming out now raises the camera
    // and gently steepens the angle instead of just enlarging the frustum, which both looks more
    // natural and directly helps with terrain clipping (you're physically higher up when zoomed out).
    [Header("Zoom range - farthest-zoom values (zoomLevel = 1)")]
    public float maxHeightAboveStore = 25f;
    public float maxLookAngle = 50f; // slightly steeper than lookAngle - keeps framing balanced at a distance

    [Header("Terrain clearance - prevents clipping into terrain while panning across uneven ground")]
    public Terrain terrain;
    public float minClearanceAboveTerrain = 2f;

    [Header("Movement (WASD)")]
    public float moveSpeed = 20f;
    public float fastMoveMultiplier = 2f; // hold Left Shift to move faster

    // Update: now driven directly by zoomLevel (0-1) instead of orthographic size, since
    // orthographic size is no longer what zoom controls.
    [Header("Zoom-based speed scaling - more zoomed OUT = faster")]
    public float minZoomSpeedMultiplier = 0.3f;
    public float maxZoomSpeedMultiplier = 5f;

    [Header("Pan (hold left OR right mouse button + drag)")]
    public float panSpeed = 1f;

    [Header("Zoom (scroll wheel) - raises height + steepens angle, does NOT resize the orthographic frustum")]
    public Camera cam;
    public float zoomSpeed = 0.5f; // how much zoomLevel (0-1) shifts per scroll tick - default gives ~20 notches for the full range

    float zoomLevel = 0f; // 0 = fully zoomed in, 1 = fully zoomed out

    Vector3 lastMousePosition;

    void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
        cam.orthographic = false;

        zoomLevel = 0f; // start fully zoomed in
        if (storeTransform != null) SnapToStore();
    }

    // Update: now routes through ApplyZoomLevel() so the initial framing and every later zoom
    // change use exactly the same height/pitch math - no duplicated logic to drift out of sync.
    public void SnapToStore()
    {
        ApplyZoomLevel(); // sets rotation (pitch+yaw) and height, based on current zoomLevel

        float currentHeight = Mathf.Lerp(heightAboveStore, maxHeightAboveStore, zoomLevel);
        float currentPitch = Mathf.Lerp(lookAngle, maxLookAngle, zoomLevel);

        float yawRadians = transform.eulerAngles.y * Mathf.Deg2Rad;
        Vector3 flatForward = new Vector3(Mathf.Sin(yawRadians), 0f, Mathf.Cos(yawRadians));

        float pitchRadians = currentPitch * Mathf.Deg2Rad;
        float horizontalDistance = currentHeight / Mathf.Tan(pitchRadians); // exact distance so the view lands on the store

        Vector3 target = storeTransform.position - flatForward * horizontalDistance;
        transform.position = new Vector3(target.x, transform.position.y, target.z); // Y already set correctly by ApplyZoomLevel
    }

    void Update()
    {
        HandleMovement();
        HandlePan();
        HandleZoom();
    }

    // Update: this is the actual clipping fix. Height was only ever set correctly ONCE,
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

    // Update: reads zoomLevel (0-1) directly now instead of orthographic size, since zoom no
    // longer touches orthographic size at all.
    float CurrentZoomSpeedMultiplier() => Mathf.Lerp(minZoomSpeedMultiplier, maxZoomSpeedMultiplier, zoomLevel);

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S
        float speed = moveSpeed * CurrentZoomSpeedMultiplier() * (Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f);

        Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
        Vector3 right = transform.right; right.y = 0; right.Normalize();

        Vector3 move = (forward * v + right * h) * speed * Time.deltaTime;
        move.y = 0f; // WASD never touches height directly - only zoom does, via ApplyZoomLevel
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
            Vector3 forwardFlat = Vector3.Cross(right, Vector3.up); // flat forward, ignoring the current tilt

            Vector3 move = (-right * delta.x - forwardFlat * delta.y) * panSpeed * Time.deltaTime;
            move.y = 0f; // height is never touched by panning - only zoom changes it

            transform.position += move;
            lastMousePosition = Input.mousePosition;
        }
    }

    // Update: full rewrite - zoom no longer resizes the orthographic frustum at all. Scrolling
    // out now raises the camera's height and gently steepens its pitch instead, which looks more
    // like a real camera pulling back and also directly helps avoid terrain clipping, since the
    // camera is physically higher up whenever it's zoomed out.
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;

        zoomLevel = Mathf.Clamp01(zoomLevel - scroll * zoomSpeed); // scrolling up (positive) zooms in -> lowers zoomLevel
        ApplyZoomLevel();
    }

    // Update: NEW - shared by SnapToStore() and HandleZoom() so both use identical math.
    // Only touches rotation and height (Y) - horizontal position (X/Z) is left exactly where
    // pan/WASD put it, so zooming never yanks the view sideways.
    void ApplyZoomLevel()
    {
        float currentHeight = Mathf.Lerp(heightAboveStore, maxHeightAboveStore, zoomLevel);
        float currentPitch = Mathf.Lerp(lookAngle, maxLookAngle, zoomLevel);

        transform.rotation = Quaternion.Euler(currentPitch, yaw, 0f); // yaw never changes, only pitch does with zoom

        Vector3 pos = transform.position;
        pos.y = (storeTransform != null ? storeTransform.position.y : 0f) + currentHeight;
        transform.position = pos;
    }
}