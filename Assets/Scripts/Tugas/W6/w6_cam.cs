using UnityEngine;

public class w6_cam : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target;           // Drag your Hero (Lady Pirate) here
    public Vector3 offset = new Vector3(0f, 2f, -5f); // Distance from hero

    [Header("Rotation Settings")]
    public float mouseSensitivity = 3f;
    public float pitchMin = -20f;      // Max downward angle look
    public float pitchMax = 60f;       // Max upward angle look

    [Header("Raycast / Collision Settings")]
    public LayerMask collisionLayers;  // Check obstacle/ground layers
    public float minDistance = 1f;     // Nearest camera can get to target
    public float cameraRadius = 0.2f;  // Radius for sphere casting

    private float currentYaw = 0f;
    private float currentPitch = 15f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Initialize camera angles based on target orientation
        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Handle Camera Rotation via Mouse Inputs
        if (Input.GetMouseButton(1)) // Hold Right Mouse Button to Orbit (or remove condition for always-active orbit)
        {
            currentYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            currentPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);
        }

        // Calculate Rotation Quaternion
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // 2. Determine Ideal Position
        Vector3 targetPivot = target.position + Vector3.up * offset.y;
        Vector3 idealCamPos = targetPivot + rotation * new Vector3(0f, 0f, offset.z);

        // 3. Raycast / SphereCast Collision Detection
        Vector3 rayDirection = idealCamPos - targetPivot;
        float maxDistance = Mathf.Abs(offset.z);

        // SphereCast checks if terrain/walls block the view between target and camera
        if (Physics.SphereCast(targetPivot, cameraRadius, rayDirection.normalized, out RaycastHit hit, maxDistance, collisionLayers))
        {
            float hitDistance = Mathf.Clamp(hit.distance, minDistance, maxDistance);
            transform.position = targetPivot + rayDirection.normalized * hitDistance;
        }
        else
        {
            transform.position = idealCamPos;
        }

        // 4. Point Camera at Target Pivot
        transform.LookAt(targetPivot);
    }
}