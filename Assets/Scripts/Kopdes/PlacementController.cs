using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    public Transform cityCenter;
    public GameObject ghostBuildingPrefab; // a semi-transparent version of your store, for preview only
    public LayerMask terrainLayerMask;     // set this to ONLY your terrain's layer, so the ghost itself never blocks its own raycast

    [Header("Category thresholds - distance from city center")]
    public float mainRoadRadius = 30f;  // inside this radius = MainRoad
    public float midRangeRadius = 70f;  // inside this (but outside mainRoadRadius) = Midrange
    // anything beyond midRangeRadius = Remote

    public LotData mainRoadLotData;
    public LotData midRangeLotData;
    public LotData remoteLotData;

    [Header("Terrain validity - keeps the store off cliffs/steep slopes")]
    public float maxSlopeAngle = 15f;       // degrees - beyond this, the ground is too steep to build on
    public float maxHeightDifference = 1.5f; // max allowed height gap between footprint corners and center - catches ledges/cliff edges
    public Vector3[] footprintCheckOffsets = new Vector3[]
    {
        new Vector3(3f, 0, 3f), new Vector3(-3f, 0, 3f), new Vector3(3f, 0, -3f), new Vector3(-3f, 0, -3f)
        // TODO: tune these to roughly match your actual store prefab's footprint size/2 in each direction -
        // these represent the store's four corners relative to its center, so customer entry/exit points
        // (which live near the edges of the store, per Section 5) are guaranteed to sit on walkable ground too.
    };

    [Header("Ghost feedback")]
    public Renderer ghostRenderer;   // the ghost's own Renderer, for tinting valid/invalid
    public Material validMaterial;
    public Material invalidMaterial;

    public LotManager lotManager;
    public MapPlacementUI placementUI; // optional floating label near the cursor - no confirm button anymore

    [Header("Click vs drag + confirmation modal")]
    public MapCameraController cameraController; // its Drag Start Threshold decides what counts as a click
    public PlacementConfirmUI confirmUI;         // the confirm / cancel modal

    GameObject ghostInstance;
    Vector3 currentHitPoint;
    LotData currentCategory;
    bool hasValidPlacement;

    // Click detection (a click = press + release without moving past the drag threshold)
    bool pressActive;        // a left press that started on the map (not on UI) and hasn't been released yet
    Vector2 pressScreenPos;
    float pressMaxTravel;

    // What the modal is asking about - frozen when it opens, so moving the mouse to the buttons can't change it
    Vector3 pendingPoint;
    LotData pendingCategory;

    void Start()
    {
        ghostInstance = Instantiate(ghostBuildingPrefab);
        if (ghostRenderer == null) ghostRenderer = ghostInstance.GetComponentInChildren<Renderer>();
    }

    void Update()
    {
        // While the confirmation modal is open the ghost stays frozen on the spot being confirmed.
        if (PlacementConfirmUI.IsOpen) return;

        UpdateGhostPosition();
        HandleClick();
    }

    // A click = press and release WITHOUT travelling past the drag threshold. Anything further is a drag,
    // and drags belong to the camera, so they never place anything.
    void HandleClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // IsPointerOverGameObject() stops a press on UI (like a settings button) from placing underneath it.
            pressActive = !EventSystem.current.IsPointerOverGameObject();
            pressScreenPos = Input.mousePosition;
            pressMaxTravel = 0f;
        }

        if (!pressActive) return;

        pressMaxTravel = Mathf.Max(pressMaxTravel, ((Vector2)Input.mousePosition - pressScreenPos).magnitude);

        if (Input.GetMouseButtonUp(0))
        {
            pressActive = false;
            float threshold = cameraController != null ? cameraController.dragStartThreshold : 6f;
            if (pressMaxTravel < threshold && hasValidPlacement)
                OpenConfirmation();
        }
    }

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, terrainLayerMask))
        {
            currentHitPoint = hit.point;
            ghostInstance.SetActive(true);
            ghostInstance.transform.position = hit.point;

            bool terrainValid = IsSlopeAcceptable(hit.normal) && IsFootprintFlat(hit.point);
            hasValidPlacement = terrainValid;
            UpdateGhostVisual(terrainValid);

            float dist = Vector3.Distance(hit.point, cityCenter.position);
            currentCategory = ResolveCategory(dist);

            if (placementUI != null) placementUI.UpdatePreview(currentCategory, terrainValid);
        }
        else
        {
            // Mouse isn't over the terrain at all (past the map edge, or pointing at the sky) - hide the ghost and block placement
            ghostInstance.SetActive(false);
            hasValidPlacement = false;
            if (placementUI != null) placementUI.HidePreview();
        }
    }

    bool IsSlopeAcceptable(Vector3 surfaceNormal) => Vector3.Angle(surfaceNormal, Vector3.up) <= maxSlopeAngle;

    bool IsFootprintFlat(Vector3 center)
    {
        foreach (var offset in footprintCheckOffsets)
        {
            Vector3 checkPos = center + offset;
            Ray downRay = new Ray(checkPos + Vector3.up * 50f, Vector3.down);

            // Corner doesn't hit terrain at all - likely off the edge of the map. Invalid.
            if (!Physics.Raycast(downRay, out RaycastHit cornerHit, 200f, terrainLayerMask)) return false;

            // Corner sits on ground steeper than the center point allows. Invalid.
            if (!IsSlopeAcceptable(cornerHit.normal)) return false;

            // Corner is noticeably higher/lower than the center - a cliff edge or ledge cutting through
            // the footprint, which would leave part of the store floating or clipped into terrain. Invalid.
            if (Mathf.Abs(cornerHit.point.y - center.y) > maxHeightDifference) return false;
        }
        return true; // center and all four corners check out - customers can walk in/out cleanly
    }

    void UpdateGhostVisual(bool valid)
    {
        if (ghostRenderer != null) ghostRenderer.material = valid ? validMaterial : invalidMaterial;
    }

    LotData ResolveCategory(float distanceFromCity)
    {
        if (distanceFromCity <= mainRoadRadius) return mainRoadLotData;
        if (distanceFromCity <= midRangeRadius) return midRangeLotData;
        return remoteLotData;
    }

    // Freeze what was clicked and ask. If no modal is wired, behave like the old version and commit at once.
    void OpenConfirmation()
    {
        pendingPoint = currentHitPoint;
        pendingCategory = currentCategory;

        if (placementUI != null) placementUI.HidePreview(); // the hover label would otherwise sit frozen under the modal

        if (confirmUI == null)
        {
            ConfirmPlacement();
            return;
        }

        confirmUI.Show(pendingCategory, ConfirmPlacement, CancelPlacement);
    }

    void ConfirmPlacement()
    {
        lotManager.ConfirmLotSelection(pendingPoint, pendingCategory);
        // ConfirmLotSelection stores the position in GameSessionData and loads MainGameplay immediately - the
        // day starts automatically via GameplaySceneBootstrapper -> DayManager.StartGame(), no extra step needed.
    }

    void CancelPlacement()
    {
        // Nothing to undo: the modal has closed, and next frame the ghost simply follows the cursor again.
    }
}