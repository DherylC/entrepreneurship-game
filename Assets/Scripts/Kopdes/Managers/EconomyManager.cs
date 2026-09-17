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
        }
        store.queuedOrder = null;
    }

    public static bool PlaceOrder(StoreState store, Dictionary<ItemData, int> orderedItems, GameBalanceConfig config)
    {
        float itemCost = 0f;
        foreach (var kvp in orderedItems) itemCost += kvp.Key.flatRestockCost * kvp.Value;
        float totalCost = itemCost + config.flatDeliveryFee;

        if (store.cash < totalCost) return false; // balance only ever gets touched below this line

        store.cash -= totalCost;
        store.queuedOrder = new PendingOrder { items = orderedItems };
        return true;
    }

    public static int CustomersForHour(StoreState store, LotData lotData, GameBalanceConfig config, float eventMultiplierThisHour, int totalOpenHours)
    {
        float locationTraffic = store.lot.GetTrafficMultiplier(lotData);
        float facilityBonus = store.upgrades.FacilityTrafficBonus(config);
        float baseCustomersPerHour = (config.baseCustomersPerDay * locationTraffic * facilityBonus) / totalOpenHours;
        return Mathf.RoundToInt(baseCustomersPerHour * eventMultiplierThisHour);
    }

    // FIXED: now takes currentDay so locked items (unlockOnDay in the future) are never picked/sold.
    public static void ResolveHourOfSales(StoreState store, DayResults results, int customerCount, GameBalanceConfig config, int currentDay)
    {
        results.totalCustomers += customerCount;
        float paymentBonus = store.upgrades.PaymentBonusMultiplier(config);

        for (int i = 0; i < customerCount; i++)
        {
            ItemData item = PickRandomItem(store, currentDay);
            if (item == null) continue; // nothing unlocked/stocked yet - customer leaves empty-handed

            if (store.currentStock[item] > 0)
            {
                store.currentStock[item]--;
                float roll = Random.Range(config.minSellMultiplier, config.maxSellMultiplier);
                float salePrice = item.baseSellPrice * roll * paymentBonus;
                store.cash += salePrice;
                results.revenue += salePrice;
                results.itemsSold[item] = results.itemsSold.GetValueOrDefault(item) + 1;
            }
            else
            {
                results.complaints++;
            }
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