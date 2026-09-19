using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameObject shoppingPanel, upgradePanel, inventoryPanel, closingSummaryPanel, tutorialBanner;
    public GameObject inGameUI; // parent object holding the Shop/Inventory/Upgrade HUD buttons
    public GameObject gameOverPanel; // Update: the "Store Closed" screen

    public bool PlayerReadyForNextDay; // DayManager waits on this

    void Start()
    {
        shoppingPanel.SetActive(false);
        upgradePanel.SetActive(false);
        inventoryPanel.SetActive(false);
        closingSummaryPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        inGameUI.SetActive(true); // HUD buttons visible by default, panels are not
    }

    void HideAll()
    {
        shoppingPanel.SetActive(false);
        upgradePanel.SetActive(false);
        inventoryPanel.SetActive(false);
        closingSummaryPanel.SetActive(false);
    }

    public void ShowShopping()
    {
        HideAll();
        inGameUI.SetActive(false);
        shoppingPanel.SetActive(true);
        shoppingPanel.GetComponent<ShoppingUI>().OpenPanel(); // triggers BuildRows()
    }

    public void ShowUpgrades()
    {
        HideAll();
        inGameUI.SetActive(false);
        upgradePanel.SetActive(true);
        upgradePanel.GetComponent<UpgradeUI>().OpenPanel();
    }

    public void ShowInventory()
    {
        HideAll();
        inGameUI.SetActive(false);
        inventoryPanel.SetActive(true);
        inventoryPanel.GetComponent<InventoryUI>().OpenPanel();
    }

    public void ShowClosingSummary(StoreState store, DayResults results)
    {
        HideAll();
        inGameUI.SetActive(false);
        closingSummaryPanel.SetActive(true);
        closingSummaryPanel.GetComponent<ClosingSummaryUI>().Populate(store, results);
    }

    // Update: no HideAll() call needed here - HandleHourTick already returns immediately
    // after calling this, so no other panel could still be open at this point anyway.
    public void ShowGameOver()
    {
        inGameUI.SetActive(false);
        gameOverPanel.SetActive(true);
    }

    public void CloseCurrentPanel()
    {
        HideAll();
        inGameUI.SetActive(true); // bring the HUD buttons back once a panel is dismissed
    }

    public void ShowTutorialMessage(string message)
    {
        tutorialBanner.SetActive(true);
        tutorialBanner.GetComponentInChildren<TMP_Text>().text = message;
    }

    public void HideTutorialMessage() => tutorialBanner.SetActive(false);
}