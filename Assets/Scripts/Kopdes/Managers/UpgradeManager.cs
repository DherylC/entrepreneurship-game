public static class UpgradeManager
{
    public static bool TryUpgrade(StoreState store, GameBalanceConfig config, UpgradeTrack track)
    {
        int currentLevel = GetLevel(store, track);
        if (currentLevel >= config.maxUpgradeLevel) return false;

        UpgradeCurve curve = GetCurve(config, track);
        float cost = curve.CostForLevel(currentLevel);
        if (store.cash < cost) return false;

        store.cash -= cost;
        CashEvents.Raise(-cost);
        SetLevel(store, track, currentLevel + 1);
        return true;
    }

    static int GetLevel(StoreState s, UpgradeTrack t) => t switch
    {
        UpgradeTrack.Capacity => s.upgrades.capacityLevel,
        UpgradeTrack.Decor => s.upgrades.decorLevel,
        UpgradeTrack.Facility => s.upgrades.facilityLevel,
        _ => 0
    };

    static void SetLevel(StoreState s, UpgradeTrack t, int level)
    {
        if (t == UpgradeTrack.Capacity) s.upgrades.capacityLevel = level;
        if (t == UpgradeTrack.Decor) s.upgrades.decorLevel = level;
        if (t == UpgradeTrack.Facility) s.upgrades.facilityLevel = level;
    }

    static UpgradeCurve GetCurve(GameBalanceConfig c, UpgradeTrack t) => t switch
    {
        UpgradeTrack.Capacity => c.capacityCurve,
        UpgradeTrack.Decor => c.decorCurve,
        UpgradeTrack.Facility => c.facilityCurve,
        _ => null
    };
}

public enum UpgradeTrack { Capacity, Decor, Facility }