using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    public UpgradeTrack track;
    public TMP_Text levelText;
    public TMP_Text costText;
    public TMP_Text effectPreviewText;
    public Button purchaseButton;

    UpgradeUI parentUI;

    public void Refresh(StoreState store, GameBalanceConfig config, UpgradeUI owner)
    {
        parentUI = owner;

        int currentLevel = GetLevel(store);
        bool atMax = currentLevel >= config.maxUpgradeLevel;

        levelText.text = $"Lv. {currentLevel}";
        purchaseButton.onClick.RemoveAllListeners();

        if (atMax)
        {
            costText.text = "MAX";
            purchaseButton.interactable = false;
        }
        else
        {
            UpgradeCurve curve = GetCurve(config);
            float cost = curve.CostForLevel(currentLevel);
            costText.text = $"Rp {cost:F0}";
            purchaseButton.interactable = store.cash >= cost;
            purchaseButton.onClick.AddListener(() => parentUI.OnUpgradePurchased(track));

            effectPreviewText.text = BuildEffectPreview(track, curve, currentLevel);
        }
    }

    int GetLevel(StoreState s) => track switch
    {
        UpgradeTrack.Capacity => s.upgrades.capacityLevel,
        UpgradeTrack.Decor => s.upgrades.decorLevel,
        UpgradeTrack.Facility => s.upgrades.facilityLevel,
        _ => 0
    };

    UpgradeCurve GetCurve(GameBalanceConfig c) => track switch
    {
        UpgradeTrack.Capacity => c.capacityCurve,
        UpgradeTrack.Decor => c.decorCurve,
        UpgradeTrack.Facility => c.facilityCurve,
        _ => null
    };

    string BuildEffectPreview(UpgradeTrack track, UpgradeCurve curve, int currentLevel)
    {
        float current = curve.EffectAtLevel(currentLevel);
        float next = curve.EffectAtLevel(currentLevel + 1);
        return track switch
        {
            UpgradeTrack.Capacity => $"Storage: {current:F0} -> {next:F0}",
            UpgradeTrack.Decor => $"Payment bonus: +{current * 100:F0}% -> +{next * 100:F0}%",
            UpgradeTrack.Facility => $"Traffic bonus: +{current * 100:F0}% -> +{next * 100:F0}%",
            _ => ""
        };
    }
}