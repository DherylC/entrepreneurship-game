using UnityEngine;

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
    public float maxHeightSpeedMultiplier = 5f;

    [Header("Drag-pan (hold right mouse button) - moves position only, never rotates")]
    public float dragPanSpeed = 1f;
    Vector3 lastMousePosition;

    [Header("Zoom (scroll wheel zooms toward the cursor; keys and buttons zoom toward the screen centre)")]
    public float zoomPerNotch = 3f;        // world units per scroll notch at referenceHeight; scales with height
    public float minHeight = 8f;           // closest the camera may get (world Y)
    public float maxHeight = 80f;          // farthest
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

    // Update: this replaces the old rotate-on-drag "look" system entirely. Right-click-drag
    // now just slides the camera along X/Z in the drag direction - the camera's rotation is
    // fixed at Start() and is never touched again, matching a Clash of Clans-style camera.
    void HandleDragPan()
    {
        if (Input.GetMouseButtonDown(1)) lastMousePosition = Input.mousePosition;

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 right = transform.right;
            Vector3 forwardFlat = Vector3.Cross(right, Vector3.up); // flat forward, ignoring the fixed downward tilt

            Vector3 move = (-right * delta.x - forwardFlat * delta.y) * dragPanSpeed * CurrentHeightMultiplier() * Time.deltaTime;
            move.y = 0f; // drag-panning never changes height - that's zoom's job

            transform.position += move;
            lastMousePosition = Input.mousePosition;
        }
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