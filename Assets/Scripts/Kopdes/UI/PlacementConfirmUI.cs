using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlacementConfirmUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("The modal: a full-screen dimmer (Raycast Target ON) with the dialog inside. Leave it inactive in the scene.")]
    public GameObject panelRoot;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Keyboard shortcuts (change freely)")]
    public KeyCode confirmKey = KeyCode.Return;
    public KeyCode cancelKey = KeyCode.Escape;

    Action onConfirm;
    Action onCancel;

    void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);

        panelRoot.SetActive(false);
        IsOpen = false;
    }

    // IsOpen is static, so don't leave it stuck on true if the scene unloads while the modal is showing.
    void OnDestroy() => IsOpen = false;

    public void Show(LotData lot, Action confirmCallback, Action cancelCallback)
    {
        onConfirm = confirmCallback;
        onCancel = cancelCallback;

        panelRoot.SetActive(true);
        IsOpen = true;
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