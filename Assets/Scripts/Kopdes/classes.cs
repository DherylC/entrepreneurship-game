using System.Collections.Generic;
using UnityEngine;

public class StoreState
{
    public float cash;
    public Dictionary<ItemData, int> currentStock = new();
    public StoreUpgrades upgrades = new();
    public PendingOrder queuedOrder; // null if player skipped ordering
    public LotSite lot;
    public int baseCustomersToday; // grows by a random 5-10 each day, set initially in StartGame(), incremented in AdvanceDay()
}

public class StoreUpgrades
{
    public int capacityLevel;
    public int decorLevel;
    public int facilityLevel;

    public int MaxStockPerItem(GameBalanceConfig config)
        => Mathf.RoundToInt(config.capacityCurve.EffectAtLevel(capacityLevel));

    public float PaymentBonusMultiplier(GameBalanceConfig config)
        => 1f + (float)config.decorCurve.EffectAtLevel(decorLevel);

    public float FacilityTrafficBonus(GameBalanceConfig config)
        => 1f + (float)config.facilityCurve.EffectAtLevel(facilityLevel);
}

public class PendingOrder
{
    public Dictionary<ItemData, int> items = new();
}

public class DayResults
{
    public int totalCustomers;
    public Dictionary<ItemData, int> itemsSold = new();
    public float revenue;
    public int complaints;
    public WeatherEventData triggeredEvent;
    public int eventTriggerHour = -1;
    public bool theftOccurred;
    public int theftTriggerHour = -1; // Update: the specific hour theft actually starts, mirrors eventTriggerHour
    public bool theftResolved;        // Update: true once caught or escaped, so notifications/spawns don't double-fire
    public bool theftCaught;
}

public class LotSite
{
    public Vector2 position;
    public float normalizedProximity; // 0 = far from city, 1 = right next to it, set once at placement

    public float GetTrafficMultiplier(LotData data) => Mathf.Lerp(data.minTrafficMultiplier, data.maxTrafficMultiplier, normalizedProximity);
    public float GetPriceMultiplier(LotData data) => Mathf.Lerp(data.minPriceMultiplier, data.maxPriceMultiplier, normalizedProximity);
    public float GetEventChanceMultiplier(LotData data) => Mathf.Lerp(data.minEventChanceMultiplier, data.maxEventChanceMultiplier, normalizedProximity);
}