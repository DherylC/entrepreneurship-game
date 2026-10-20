using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlacementConfirmUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("The modal: a full-screen dimmer (Raycast Target ON) with the dialog inside. Leave it inactive in the scene.")]
    public GameObject panelRoot;
    public TMP_Text titleText;
    public TMP_Text detailText;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Keyboard shortcuts (change freely)")]
    public KeyCode confirmKey = KeyCode.Return;
    public KeyCode cancelKey = KeyCode.Escape;

    Action onConfirm;
    Action onCancel;
    bool listenersAdded;

    void Awake()
    {
        AddListeners();

        // Hide the modal at scene start. The IsOpen check matters: if this script sits ON the modal panel,
        // Awake first runs at the moment Show() activates it, and it must not hide the modal being shown.
        // (A root object here would be the Canvas itself - hiding that would hide the whole UI, so never do it.)
        if (!IsOpen && panelRoot != null && panelRoot.transform.parent != null) panelRoot.SetActive(false);
    }

    void AddListeners()
    {
        if (listenersAdded) return;
        listenersAdded = true;

        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
    }

    // IsOpen is static, so don't leave it stuck on true if the scene unloads while the modal is showing.
    void OnDestroy() => IsOpen = false;

    public void Show(LotData lot, Action confirmCallback, Action cancelCallback)
    {
        onConfirm = confirmCallback;
        onCancel = cancelCallback;

        AddListeners(); // in case Awake hasn't run yet (script on an object that started inactive)

        // Panel Root wired to the Canvas itself is the classic mistake: SetActive(true) on an already-active Canvas
        // does nothing, so the modal never appears while IsOpen would freeze the camera and ghost. Refuse loudly.
        if (panelRoot == null || panelRoot.transform.parent == null)
        {
            Debug.LogError("PlacementConfirmUI: Panel Root is empty or is a root object (the Canvas itself?). Drag the ConfirmModal object, a CHILD of the Canvas, into Panel Root.", this);
            onCancel?.Invoke();
            return;
        }

        titleText.text = "Build Here?";
        detailText.text = lot != null
            ? $"Shop location CANNOT be changed during the game.\nTraffic: {lot.maxTrafficMultiplier:F1}x"
            : "";

        IsOpen = true;            // set BEFORE activating - see Awake
        panelRoot.SetActive(true);

        // Fail-safe: switched on but still hidden means a PARENT of Panel Root is inactive. Don't leave the
        // camera and ghost frozen behind a modal nobody can see.
        if (!panelRoot.activeInHierarchy)
        {
            Debug.LogError("PlacementConfirmUI: Panel Root is switched on but still inactive in the Hierarchy - one of its parents is inactive. Make Panel Root a direct child of the Canvas (not inside the hover label or another hidden object).", panelRoot);
            IsOpen = false;
            onCancel?.Invoke();
        }
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
        else if (Input.GetKeyDown(cancelKey)) Cancel();
    }

    void Confirm()
    {
        Close();
        onConfirm?.Invoke();
    }

    void Cancel()
    {
        Close();
        onCancel?.Invoke();
    }

    void Close()
    {
        panelRoot.SetActive(false);
        IsOpen = false;
    }
}