using System.Collections.Generic;
using UnityEngine;

public static class EconomyManager
{
    // FIXED: previously referenced a non-existent GameContext.Config - now takes config as a real parameter,
    // consistent with every other method in this class.
    public static void ResolveDelivery(StoreState store, GameBalanceConfig config)
    {
        if (store.queuedOrder == null) return;
        foreach (var kvp in store.queuedOrder.items)
        {
            int cap = store.upgrades.MaxStockPerItem(config);
            store.currentStock[kvp.Key] = Mathf.Min(store.currentStock.GetValueOrDefault(kvp.Key) + kvp.Value, cap);
            StockEvents.Raise();
        }
        store.queuedOrder = null;
    }

    // Update: now returns bool and only deducts cash if the order is actually affordable -
    // previously this deducted unconditionally and trusted the caller to have already checked,
    // which was a real gap (nothing stopped cash from going negative if a caller skipped the check).
    public static bool PlaceOrder(StoreState store, System.Collections.Generic.Dictionary<ItemData, int> orderedItems, GameBalanceConfig config)
    {
        float itemCost = 0f;
        foreach (var kvp in orderedItems) itemCost += kvp.Key.flatRestockCost * kvp.Value;
        float totalCost = itemCost + config.flatDeliveryFee;

        if (store.cash < totalCost) return false; // balance is never touched below this line

        store.cash -= totalCost;
        CashEvents.Raise(-totalCost);
        store.queuedOrder = new PendingOrder { items = orderedItems };
        return true;
    }

    // Update: now uses store.baseCustomersToday (grows day over day) instead of the static
    // config.baseCustomersPerDay - the config value is now only the DAY 1 starting point.
    public static int CustomersForHour(StoreState store, LotData lotData, GameBalanceConfig config, float eventMultiplierThisHour, int totalOpenHours)
    {
        float locationTraffic = store.lot.GetTrafficMultiplier(lotData);
        float facilityBonus = store.upgrades.FacilityTrafficBonus(config);
        float baseCustomersPerHour = (store.baseCustomersToday * locationTraffic * facilityBonus) / totalOpenHours;
        return Mathf.RoundToInt(baseCustomersPerHour * eventMultiplierThisHour);
    }

    // Update: kept as a thin wrapper around ResolveSingleCustomer() below - still used by
    // TestHarness.Test_SimulateOneHour for a quick instant-bulk debug check, but DayManager
    // itself no longer calls this directly (see the gradual-resolution rewrite in Section 3).
    public static void ResolveHourOfSales(StoreState store, DayResults results, int customerCount, GameBalanceConfig config, int currentDay)
    {
        for (int i = 0; i < customerCount; i++)
            ResolveSingleCustomer(store, results, config, currentDay);
    }

    // Update: extracted from the old ResolveHourOfSales loop body so DayManager can call this
    // once per customer, spaced out over real time, instead of resolving a whole hour at once.
    // Also folds in the CashEvents.Raise(salePrice) call from the Cash HUD section - one sale,
    // one event, one popup.
    public static void ResolveSingleCustomer(StoreState store, DayResults results, GameBalanceConfig config, int currentDay)
    {
        results.totalCustomers++;
        float paymentBonus = store.upgrades.PaymentBonusMultiplier(config);

        ItemData item = PickRandomItem(store, currentDay);
        if (item == null) return; // nothing unlocked/stocked yet - customer leaves empty-handed

        if (store.currentStock[item] > 0)
        {
            store.currentStock[item]--;
            StockEvents.Raise();
            float roll = Random.Range(config.minSellMultiplier, config.maxSellMultiplier);
            float salePrice = item.baseSellPrice * roll * paymentBonus;
            store.cash += salePrice;
            CashEvents.Raise(salePrice);
            results.revenue += salePrice;
            results.itemsSold[item] = results.itemsSold.GetValueOrDefault(item) + 1;
        }
        else
        {
            results.complaints++;
        }
    }

    static ItemData PickRandomItem(StoreState store, int currentDay)
    {
        // Uniform random among unlocked items for v1; weighted popularity can come later.
        var unlockedKeys = new System.Collections.Generic.List<ItemData>();
        foreach (var key in store.currentStock.Keys)
            if (key.unlockOnDay <= currentDay) unlockedKeys.Add(key);

        if (unlockedKeys.Count == 0) return null;
        return unlockedKeys[Random.Range(0, unlockedKeys.Count)];
    }
}