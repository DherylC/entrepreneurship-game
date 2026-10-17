using UnityEngine;
using UnityEngine.EventSystems;

public class MapCameraController : MonoBehaviour
{
    [Header("Default position - starts near this point (e.g. CityCenter, or an existing store)")]
    public Transform focusPoint;
    public float defaultHeight = 25f;
    public float defaultDistanceBack = 20f;

    [Header("Fixed view angle - set once, never changes at runtime")]
    public float yaw = 45f;   // 45 = facing North East (+Z = North, +X = East in Unity)
    public float pitch = 55f; // downward tilt

    [Header("Movement (WASD)")]
    public float moveSpeed = 20f;
    public float fastMoveMultiplier = 2f; // hold Left Shift to move faster

    [Header("Height-based speed scaling (applies to WASD and drag-pan)")]
    public float referenceHeight = 20f;
    public float minHeightSpeedMultiplier = 0.3f;
    public float maxHeightSpeedMultiplier = 12f; // Update: raised from 5 so a big zoom range stays usable

    [Header("Drag-pan (hold LEFT or RIGHT mouse button) - the ground point you grab stays under the cursor")]
    [Tooltip("Pixels the LEFT button must travel before it counts as a drag. Below this, release = a placement click.")]
    public float dragStartThreshold = 6f;

    bool pressActive;        // a press that may become a drag is in progress (it did not start over UI)
    bool dragging;           // the threshold was crossed - the camera is following the cursor
    Vector2 pressScreenPos;
    Plane dragPlane;         // horizontal plane through the grabbed ground point
    Vector3 dragAnchor;      // the ground point that was under the cursor when the drag began

    [Header("Zoom (scroll wheel zooms toward the cursor; keys and buttons zoom toward the screen centre)")]
    public float zoomPerNotch = 3f;        // world units per scroll notch at referenceHeight; scales with height
    public float minHeight = 5f;           // closest the camera may get (world Y) - Update: bigger range by default
    public float maxHeight = 250f;         // farthest - set both in the Inspector or with SetZoomRange()
    public float zoomSmoothing = 12f;      // higher = snappier, 0 or less = instant
    public KeyCode zoomInKey = KeyCode.Equals;   // the "+" key (also KeypadPlus)
    public KeyCode zoomOutKey = KeyCode.Minus;   // also KeypadMinus
    public float keyZoomSpeed = 6f;        // world units per second while a zoom key is held, at referenceHeight
    public float buttonZoomStep = 6f;      // world units per ZoomIn()/ZoomOut() call, at referenceHeight

    float pendingZoom;                     // zoom distance still to apply, eased in over a few frames (+ = in, - = out)
    Vector2 zoomFocusScreenPoint;          // screen point the pending zoom heads toward

    [Header("Terrain boundary - keeps the CAMERA from flying past the edges (placement itself is unaffected)")]
    public Terrain terrain;
    public float boundaryMargin = 5f; // keeps the camera slightly inside the actual edge, not flush against it

    void Start()
    {
        transform.eulerAngles = new Vector3(pitch, yaw, 0f); // fixed forever - nothing else ever sets this again

        if (focusPoint != null) SnapToFocusPoint();

        // Make sure the starting height is inside the zoom limits, otherwise the first scroll would jump.
        Vector3 startPos = transform.position;
        startPos.y = Mathf.Clamp(startPos.y, minHeight, maxHeight);
        transform.position = startPos;
    }

    // Positions the camera a fixed distance behind (opposite the view direction) and above
    // whatever focusPoint is wired to - call this again manually if you ever want to "re-center"
    // the camera later (e.g. a "recenter" button).
    public void SnapToFocusPoint()
    {
        Vector3 backOffset = -transform.forward * defaultDistanceBack;
        Vector3 target = focusPoint.position + backOffset;
        transform.position = new Vector3(target.x, defaultHeight, target.z);
    }

    void Update()
    {
        // Update: no camera input at all while the placement confirmation modal is open.
        if (PlacementConfirmUI.IsOpen)
        {
            pressActive = false;
            dragging = false;
            return;
        }

        HandleMovement();
        HandleDragPan();
        HandleZoom();
        ClampToTerrainBounds(); // Update: runs last, after all movement/zoom this frame - clamps the CAMERA only
    }

    // Update: keeps the camera's own position within the terrain's actual bounds, regardless of
    // which input moved it there (WASD, drag, or zoom). This does NOT affect PlacementController's
    // raycast at all - the mouse can still hit and place on any part of terrain visible on screen,
    // even near an edge, since that's driven by the mouse ray, not by where the camera itself sits.
    void ClampToTerrainBounds()
    {
        if (terrain == null) return;

        Vector3 size = terrain.terrainData.size;
        float minX = terrain.transform.position.x + boundaryMargin;
        float maxX = terrain.transform.position.x + size.x - boundaryMargin;
        float minZ = terrain.transform.position.z + boundaryMargin;
        float maxZ = terrain.transform.position.z + size.z - boundaryMargin;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }

    float CurrentHeightMultiplier() =>
        Mathf.Clamp(transform.position.y / referenceHeight, minHeightSpeedMultiplier, maxHeightSpeedMultiplier);

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S
        float speed = moveSpeed * CurrentHeightMultiplier() * (Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f);

        // Move relative to the camera's own flat forward/right (ignoring pitch), so WASD
        // always moves along the ground plane even with the camera tilted downward.
        Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
        Vector3 right = transform.right; right.y = 0; right.Normalize();

        transform.position += (forward * v + right * h) * speed * Time.deltaTime;
    }

    // Update: grab-the-ground drag (the same idea as the MainGameplay camera). On press we remember the exact
    // ground point under the cursor; every frame while dragging we cast the cursor onto the same horizontal plane
    // and slide the camera by the gap, so the grabbed point stays under the cursor until release. The camera's
    // rotation is still fixed and its height is never touched (that's zoom's job).
    void HandleDragPan()
    {
        bool left = Input.GetMouseButton(0);
        bool right = Input.GetMouseButton(1);

        if (!left && !right)
        {
            pressActive = false;
            dragging = false;
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            // A press that starts over UI (buttons, panels) never drags the camera.
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            pressActive = !overUI;
            dragging = false;
            pressScreenPos = Input.mousePosition;
        }

        if (!pressActive) return;

        if (!dragging)
        {
            // The LEFT button is also the placement click, so it has to travel a few pixels before it counts as a
            // drag - a plain click must never nudge the camera. The right button only drags, so it starts at once.
            bool leftOnly = left && !right;
            if (leftOnly && ((Vector2)Input.mousePosition - pressScreenPos).magnitude < dragStartThreshold) return;

            // Anchor at the CURRENT cursor position, so there is no jump at the moment the drag starts.
            if (!TryGrabGround()) return;
            dragging = true;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(ray, out float enter)) return; // cursor aimed above the horizon this frame - skip it

        Vector3 shift = dragAnchor - ray.GetPoint(enter);
        shift.y = 0f;
        transform.position += shift;
    }

    // Finds the ground point under the cursor. Uses the terrain's own collider so a ghost building (or anything
    // else under the cursor) can't change what gets grabbed.
    bool TryGrabGround()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float planeY;
        TerrainCollider terrainCollider = terrain != null ? terrain.GetComponent<TerrainCollider>() : null;
        if (terrainCollider != null && terrainCollider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            planeY = hit.point.y;
        else
            planeY = terrain != null ? terrain.transform.position.y : 0f;

        dragPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        if (!dragPlane.Raycast(ray, out float enter)) return false;

        dragAnchor = ray.GetPoint(enter);
        return true;
    }

    // Change the zoom range at runtime (e.g. from a settings menu). The camera is pulled inside the new range at once.
    public void SetZoomRange(float newMinHeight, float newMaxHeight)
    {
        minHeight = Mathf.Max(0.5f, Mathf.Min(newMinHeight, newMaxHeight));
        maxHeight = Mathf.Max(minHeight + 1f, newMaxHeight);

        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;
    }

    // Public so on-screen +/- buttons can call them from OnClick().
    public void ZoomIn() => QueueZoom(buttonZoomStep * CurrentHeightMultiplier(), new Vector2(Screen.width, Screen.height) * 0.5f);
    public void ZoomOut() => QueueZoom(-buttonZoomStep * CurrentHeightMultiplier(), new Vector2(Screen.width, Screen.height) * 0.5f);

    void QueueZoom(float distance, Vector2 focusScreenPoint)
    {
        pendingZoom += distance;
        zoomFocusScreenPoint = focusScreenPoint;
    }

    void HandleZoom()
    {
        // Scroll wheel: GetAxis gives about +-0.1 per notch, so x10 makes a notch roughly 1. Positive = zoom in.
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            QueueZoom(scroll * 10f * zoomPerNotch * CurrentHeightMultiplier(), Input.mousePosition);

        // Keys: continuous while held.
        float keyDir = 0f;
        if (Input.GetKey(zoomInKey) || Input.GetKey(KeyCode.KeypadPlus)) keyDir += 1f;
        if (Input.GetKey(zoomOutKey) || Input.GetKey(KeyCode.KeypadMinus)) keyDir -= 1f;
        if (keyDir != 0f)
            QueueZoom(keyDir * keyZoomSpeed * CurrentHeightMultiplier() * Time.deltaTime, new Vector2(Screen.width, Screen.height) * 0.5f);

        if (Mathf.Abs(pendingZoom) < 0.001f) { pendingZoom = 0f; return; }

        // Ease in a fraction of the pending zoom each frame (frame-rate independent).
        float t = zoomSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-zoomSmoothing * Time.deltaTime);
        float step = pendingZoom * t;
        pendingZoom -= step;

        // Move along the ray through the focus point: for a perspective camera this keeps whatever is under
        // that screen point under it while zooming.
        Camera cam = Camera.main;
        Vector3 dir = (cam != null ? cam.ScreenPointToRay(zoomFocusScreenPoint).direction : transform.forward);

        Vector3 pos = transform.position;
        float newY = pos.y + dir.y * step; // dir points INTO the scene (downward, dir.y < 0), so zooming in (step > 0) lowers Y
        if (Mathf.Abs(dir.y) > 0.0001f)
        {
            float clampedY = Mathf.Clamp(newY, minHeight, maxHeight);
            if (!Mathf.Approximately(newY, clampedY))
            {
                step = (clampedY - pos.y) / dir.y; // shorten the move so it lands exactly on the limit
                pendingZoom = 0f;                  // don't keep pushing against the limit
            }
        }
        transform.position = pos + dir * step;
    }
}