using UnityEngine;
using UnityEngine.EventSystems;

public class GameplayCameraController : MonoBehaviour
{
    [Header("Default position (set at runtime once the store exists)")]
    public Transform storeTransform;
    // Update: these are now the CLOSEST-zoom values (zoomLevel = 0). Farthest-zoom values are
    // the new maxHeightAboveStore/maxLookAngle fields below - scrolling interpolates between them.
    public float heightAboveStore = 6f;
    public float lookAngle = 35f; // downward tilt in degrees - 30-45 gives the diagonal corner look, not top-down
    public float yaw = 45f; // fixed forever - 45 = northeast-ish corner

    // Update: where the camera starts when MainGameplay loads. All of these only affect the START; scrolling and dragging work from there.
    [Range(0f, 1f)] public float startZoom = 0f;  // 0 = start fully zoomed in (default), 1 = fully zoomed out. Note BuildingCutaway hides the walls/roof at low zoom.
    public Vector2 startOffset = Vector2.zero;    // shifts the starting view on the ground, relative to the camera: x = right, y = forward (metres)

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

    [Header("Drag-pan (hold left OR right mouse button) - the ground point you grab stays exactly under the cursor")]
    [Tooltip("Ignore drags that START over UI (buttons, panels). Turn this off if panning stops working - a full-screen UI Image with Raycast Target on would block every drag.")]
    public bool blockDragOverUI = true;

    [Header("Zoom (scroll wheel) - raises height + steepens angle, does NOT resize the orthographic frustum")]
    public Camera cam;
    public float zoomSpeed = 0.5f; // how much zoomLevel (0-1) shifts per scroll tick - default gives ~20 notches for the full range

    [Header("Projection")]
    public bool useOrthographic = false; // Update: false = normal 3D perspective (default). true = the old orthographic look.

    float zoomLevel = 0f; // 0 = fully zoomed in, 1 = fully zoomed out
    public float ZoomLevel => zoomLevel; // Update: read-only, used by BuildingCutaway to hide the walls/roof when zoomed in

    // Drag-pan state
    bool isDragging;
    Plane dragPlane;     // horizontal plane through the grabbed ground point
    Vector3 dragAnchor;  // the world point that was under the cursor when the drag started

    void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
        cam.orthographic = useOrthographic; // Update: was hard-coded to true, which forced orthographic every time

        zoomLevel = Mathf.Clamp01(startZoom); // Update: was hard-coded to 0 (fully zoomed in)
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

        // Update: startOffset nudges the framing so the store doesn't have to sit dead centre.
        Vector3 flatRight = new Vector3(flatForward.z, 0f, -flatForward.x);
        Vector3 target = storeTransform.position - flatForward * horizontalDistance
                         + flatRight * startOffset.x + flatForward * startOffset.y;
        transform.position = new Vector3(target.x, transform.position.y, target.z); // Y already set correctly by ApplyZoomLevel
    }

    void Update()
    {
        // Update: while any panel is open (shop, inventory, upgrades, closing summary, game over) the world
        // gets no input at all - no WASD, no drag, no scroll zoom. A drag already in progress is cancelled.
        if (UIManager.IsModalOpen)
        {
            isDragging = false;
            return;
        }

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

    // Update: full rewrite - grab-the-ground dragging. The old version moved the camera by (mouse delta x a speed),
    // which can never match the cursor exactly because how far the ground moves per pixel changes with height,
    // tilt and distance. Now: on press, remember the exact GROUND POINT under the cursor. Every frame while held,
    // cast the cursor onto the same horizontal plane and slide the camera by the gap between that point and the
    // grabbed one - so the grabbed point is always under the cursor until release. Height is never touched.
    void HandlePan()
    {
        bool pressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
        bool held = Input.GetMouseButton(0) || Input.GetMouseButton(1);

        if (!held) { isDragging = false; return; }

        if (pressed)
        {
            bool overUI = blockDragOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            isDragging = !overUI && TryGrabGround();
        }

        if (!isDragging) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(ray, out float enter)) return; // cursor aimed above the horizon this frame - skip it

        Vector3 shift = dragAnchor - ray.GetPoint(enter);
        shift.y = 0f;
        transform.position += shift;
    }

    // On press: find the ground point under the cursor. Uses the terrain's own collider only, so clicking on the
    // store, a customer or the guard still grabs the GROUND behind it rather than anchoring at roof height.
    bool TryGrabGround()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        float planeY;
        TerrainCollider terrainCollider = terrain != null ? terrain.GetComponent<TerrainCollider>() : null;
        if (terrainCollider != null && terrainCollider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            planeY = hit.point.y;
        else
            planeY = storeTransform != null ? storeTransform.position.y : 0f;

        // The camera only ever moves horizontally while dragging, so a flat plane at the grabbed height is exact.
        dragPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        if (!dragPlane.Raycast(ray, out float enter)) return false;

        dragAnchor = ray.GetPoint(enter);
        return true;
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