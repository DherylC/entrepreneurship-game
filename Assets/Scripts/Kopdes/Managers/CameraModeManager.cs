using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraModeManager : MonoBehaviour
{
    public Camera cam;
    public GameplayCameraController aerialController;
    public KeyCode toggleKey = KeyCode.E;
    public float transitionDuration = 1.2f;
    public float firstPersonFov = 70f;

    [Tooltip("Aerial-only UI to hide while in first person (e.g. shopping/upgrade buttons). Optional.")]
    public GameObject[] hideInFirstPerson;

    [Tooltip("Crosshair UI object under UICanvas. Shown only in first person, hidden otherwise.")]
    public GameObject crosshair;

    [Header("Set at runtime by GameplaySceneBootstrapper")]
    public FirstPersonController guard;

    public bool IsFirstPerson { get; private set; }

    // Update: true from the START of the fly-in to first person until the START of the fly-out, so the
    // building (see BuildingCutaway) is fully visible the whole time the camera is inside or heading inside.
    public bool ShowFullBuilding { get; private set; }

    bool transitioning;
    Vector3 savedPosition;
    Quaternion savedRotation;
    Transform savedParent;
    float savedOrthoSize;
    float savedFov;
    bool savedOrthographic;

    void Start()
    {
        if (crosshair != null) crosshair.SetActive(false); // aerial view is the default state
    }

    void Update()
    {
        // Update: a panel can pop up while you're in first person (the closing summary at closing time, or
        // game over). The cursor is locked there, so nothing could be clicked - pull the player back to the
        // aerial view so they're never stuck. No E-toggle while a panel is open either.
        if (UIManager.IsModalOpen)
        {
            if (IsFirstPerson && !transitioning && guard != null) StartCoroutine(GoToAerial());
            return;
        }

        if (!Input.GetKeyDown(toggleKey)) return;

        // Don't toggle while typing into a text field (the shop quantity box).
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && selected.GetComponent<TMP_InputField>() != null) return;

        Toggle();
    }

    // Wire a UI Button's OnClick() to this as well.
    public void Toggle()
    {
        if (transitioning || UIManager.IsModalOpen) return;
        if (guard == null)
        {
            Debug.LogWarning("CameraModeManager has no guard - is a FirstPersonController inside the StorePrefab?");
            return;
        }
        StartCoroutine(IsFirstPerson ? GoToAerial() : GoToFirstPerson());
    }

    IEnumerator GoToFirstPerson()
    {
        transitioning = true;
        ShowFullBuilding = true;

        // Save the free-cam state exactly as it is right now.
        savedPosition = cam.transform.position;
        savedRotation = cam.transform.rotation;
        savedParent = cam.transform.parent;
        savedOrthoSize = cam.orthographicSize;
        savedFov = cam.fieldOfView;
        savedOrthographic = cam.orthographic;
        aerialController.enabled = false;
        Debug.Log($"[CameraMode] Saved aerial: pos={savedPosition} rot={savedRotation.eulerAngles} ortho={cam.orthographic} size={savedOrthoSize}");

        Matrix4x4 fromMatrix = cam.projectionMatrix; // whatever the aerial camera is using right now
        Matrix4x4 perspMatrix = Matrix4x4.Perspective(firstPersonFov, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
        Transform eye = guard.eyePoint;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            cam.transform.SetPositionAndRotation(
                Vector3.Lerp(savedPosition, eye.position, e),
                Quaternion.Slerp(savedRotation, eye.rotation, e));
            cam.projectionMatrix = LerpMatrix(fromMatrix, perspMatrix, e);
            yield return null;
        }

        // Land exactly on the guard's eye and switch to a normal perspective camera.
        cam.orthographic = false;
        cam.fieldOfView = firstPersonFov;
        cam.ResetProjectionMatrix();
        cam.transform.SetParent(eye, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        SetHiddenUI(true);
        if (crosshair != null) crosshair.SetActive(true);
        guard.Enter(cam);
        IsFirstPerson = true;
        transitioning = false;
    }

    IEnumerator GoToAerial()
    {
        transitioning = true;
        ShowFullBuilding = false; // the walls/roof start fading back out as the camera climbs out

        guard.Exit(); // unlocks the cursor, shows the body again so the pull-back is visible
        SetHiddenUI(false);
        if (crosshair != null) crosshair.SetActive(false);
        cam.transform.SetParent(savedParent, true);

        Vector3 fromPos = cam.transform.position;
        Quaternion fromRot = cam.transform.rotation;
        Matrix4x4 perspMatrix = cam.projectionMatrix;
        Matrix4x4 targetMatrix;
        if (savedOrthographic)
        {
            float halfW = savedOrthoSize * cam.aspect;
            targetMatrix = Matrix4x4.Ortho(-halfW, halfW, -savedOrthoSize, savedOrthoSize, cam.nearClipPlane, cam.farClipPlane);
        }
        else
        {
            targetMatrix = Matrix4x4.Perspective(savedFov, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            cam.transform.SetPositionAndRotation(
                Vector3.Lerp(fromPos, savedPosition, e),
                Quaternion.Slerp(fromRot, savedRotation, e));
            cam.projectionMatrix = LerpMatrix(perspMatrix, targetMatrix, e);
            yield return null;
        }

        RestoreAerialState();
        aerialController.enabled = true; // its private zoomLevel was never touched
        IsFirstPerson = false;

        // Re-apply once on the next frame and log what the camera really looks like. If the two
        // logs differ, or differ from the "Saved aerial" log, another script is overwriting the camera.
        Debug.Log($"[CameraMode] Restored: pos={cam.transform.position} ortho={cam.orthographic} size={cam.orthographicSize}");
        yield return null;
        RestoreAerialState();
        Debug.Log($"[CameraMode] Next frame: pos={cam.transform.position} ortho={cam.orthographic} size={cam.orthographicSize}");
        transitioning = false;
    }

    void RestoreAerialState()
    {
        cam.transform.SetParent(savedParent, true);
        cam.transform.localScale = Vector3.one; // un-parenting from a scaled hierarchy would otherwise keep that scale
        cam.transform.SetPositionAndRotation(savedPosition, savedRotation);
        cam.orthographic = savedOrthographic; // Update: was hard-coded to true
        cam.orthographicSize = savedOrthoSize;
        cam.fieldOfView = savedFov;
        cam.ResetProjectionMatrix();
    }

    void SetHiddenUI(bool hide)
    {
        foreach (var go in hideInFirstPerson)
            if (go != null) go.SetActive(!hide);
    }

    static Matrix4x4 LerpMatrix(Matrix4x4 from, Matrix4x4 to, float t)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 16; i++) result[i] = Mathf.Lerp(from[i], to[i], t);
        return result;
    }
}