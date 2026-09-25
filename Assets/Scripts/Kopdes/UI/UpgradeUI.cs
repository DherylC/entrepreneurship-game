using UnityEngine;

public class UpgradeUI : MonoBehaviour
{
    public UpgradeCardUI capacityCard;
    public UpgradeCardUI decorCard;
    public UpgradeCardUI facilityCard;
    public DayManager dayManager;

    // Update: subscribes only while the panel is active, same pattern as InventoryUI's
    // StockEvents subscription - this is the fix for "upgrades should be clickable the moment
    // the balance becomes sufficient," not just when the panel happens to be reopened.
    void OnEnable() => CashEvents.OnCashChanged += HandleCashChanged;
    void OnDisable() => CashEvents.OnCashChanged -= HandleCashChanged;

    void HandleCashChanged(float delta) => RefreshAllCards();

    public void OpenPanel()
    {
        RefreshAllCards();
    }

    public void RefreshAllCards()
    {
        capacityCard.Refresh(dayManager.store, dayManager.config, this);
        decorCard.Refresh(dayManager.store, dayManager.config, this);
        facilityCard.Refresh(dayManager.store, dayManager.config, this);
    }

    public void OnUpgradePurchased(UpgradeTrack track)
    {
        bool success = UpgradeManager.TryUpgrade(dayManager.store, dayManager.config, track);
        if (success) RefreshAllCards(); // re-read new level/cost immediately after a purchase
        // Note: TryUpgrade's own CashEvents.Raise(-cost) call (see the Cash HUD section) will
        // ALSO trigger HandleCashChanged -> RefreshAllCards() right after this on a successful
        // purchase - harmless double-refresh, not a bug if you notice cards updating twice.
    }
}