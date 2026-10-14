using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public enum ShortcutAction { Inventory, Upgrade, Shop }

public class UIShortcuts : MonoBehaviour
{
    [Header("Default keys - change here, or at runtime with SetKey()")]
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode upgradeKey = KeyCode.U;
    public KeyCode shopKey = KeyCode.T;

    [Header("References")]
    public UIManager uiManager;                   // same object as this script
    public DayManager dayManager;                 // GameManager
    public CameraModeManager cameraModeManager;   // optional, used to ignore shortcuts in first person

    void Awake()
    {
        // A key saved earlier by SetKey() overrides the Inspector default.
        inventoryKey = LoadKey(ShortcutAction.Inventory, inventoryKey);
        upgradeKey = LoadKey(ShortcutAction.Upgrade, upgradeKey);
        shopKey = LoadKey(ShortcutAction.Shop, shopKey);
    }

    void Update()
    {
        if (!CanUseShortcuts()) return;

        if (Input.GetKeyDown(inventoryKey)) Toggle(uiManager.inventoryPanel, uiManager.ShowInventory);
        else if (Input.GetKeyDown(upgradeKey)) Toggle(uiManager.upgradePanel, uiManager.ShowUpgrades);
        else if (Input.GetKeyDown(shopKey)) Toggle(uiManager.shoppingPanel, uiManager.ShowShopping);
    }

    bool CanUseShortcuts()
    {
        if (cameraModeManager != null && cameraModeManager.IsFirstPerson) return false;

        if (uiManager.closingSummaryPanel.activeSelf || uiManager.gameOverPanel.activeSelf) return false;

        // End-of-day flow: the shop must be dismissed with its own Confirm/Skip buttons.
        if (dayManager != null && dayManager.clock.currentHour >= dayManager.config.closeHour) return false;

        // Don't fire while typing into a text field.
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && selected.GetComponent<TMP_InputField>() != null) return false;

        return true;
    }

    void Toggle(GameObject panel, System.Action show)
    {
        if (panel.activeSelf) uiManager.CloseCurrentPanel();
        else show();
    }

    // ---- Rebinding (for a future settings menu) ----

    public KeyCode GetKey(ShortcutAction action)
    {
        switch (action)
        {
            case ShortcutAction.Inventory: return inventoryKey;
            case ShortcutAction.Upgrade: return upgradeKey;
            default: return shopKey;
        }
    }

    public void SetKey(ShortcutAction action, KeyCode key)
    {
        switch (action)
        {
            case ShortcutAction.Inventory: inventoryKey = key; break;
            case ShortcutAction.Upgrade: upgradeKey = key; break;
            case ShortcutAction.Shop: shopKey = key; break;
        }
        PlayerPrefs.SetInt(PrefKey(action), (int)key);
        PlayerPrefs.Save();
    }

    static string PrefKey(ShortcutAction action) => "Shortcut_" + action;
    static KeyCode LoadKey(ShortcutAction action, KeyCode fallback) => (KeyCode)PlayerPrefs.GetInt(PrefKey(action), (int)fallback);
}