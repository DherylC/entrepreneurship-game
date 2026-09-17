using UnityEngine;

public class UpgradeUI : MonoBehaviour
{
    public UpgradeCardUI capacityCard;
    public UpgradeCardUI decorCard;
    public UpgradeCardUI facilityCard;
    public DayManager dayManager;

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
    }
}