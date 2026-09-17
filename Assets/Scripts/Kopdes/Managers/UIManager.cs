using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public GameObject shoppingPanel, upgradePanel, inventoryPanel, closingSummaryPanel, tutorialBanner;
    public GameObject inGameUI; // parent object holding shopBtn/inventoryBtn/upgradeBtn

    public bool PlayerReadyForNextDay;

    void Start()
    {
        shoppingPanel.SetActive(false);
        upgradePanel.SetActive(false);
        inventoryPanel.SetActive(false);
        closingSummaryPanel.SetActive(false);
        inGameUI.SetActive(true);
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
        shoppingPanel.GetComponent<ShoppingUI>().OpenPanel();
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

    public void CloseCurrentPanel()
    {
        HideAll();
        inGameUI.SetActive(true);
    }

    public void ShowTutorialMessage(string message)
    {
        tutorialBanner.SetActive(true);
        tutorialBanner.GetComponentInChildren<TMP_Text>().text = message;
    }

    public void HideTutorialMessage() => tutorialBanner.SetActive(false);
}