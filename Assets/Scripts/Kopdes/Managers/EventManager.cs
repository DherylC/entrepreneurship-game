using UnityEngine;

public static class EventManager
{
    // Update: guards against store.lot/lotData being null - this happens if StartGame() never
    // ran (e.g. Play was pressed directly in MainGameplay, skipping MapPlacement entirely).
    // Previously this threw a raw NullReferenceException with no indication of the real cause.
    public static WeatherEventData RollWeatherEvent(LotSite lot, LotData lotData, WeatherEventData[] possibleEvents, float baseChance)
    {
        if (lot == null || lotData == null)
        {
            Debug.LogError("RollWeatherEvent called with a null lot/lotData - StartGame() likely never ran. Always press Play from MapPlacement, not directly in MainGameplay.");
            return null;
        }

        float adjustedChance = baseChance * lot.GetEventChanceMultiplier(lotData);
        if (Random.value > adjustedChance) return null;
        return PickWeighted(possibleEvents);
    }

    public static bool RollTheftEvent(LotSite lot, LotData lotData, TheftEventData theftData)
    {
        if (lot == null || lotData == null)
        {
            Debug.LogError("RollTheftEvent called with a null lot/lotData - StartGame() likely never ran.");
            return false;
        }

        float adjustedChance = theftData.dailyChance * lot.GetEventChanceMultiplier(lotData);
        return Random.value < adjustedChance;
    }

    public static float GetCurrentMultiplier(WeatherEventData ev, int triggerHour, int currentHour)
    {
        if (ev == null || currentHour < triggerHour) return 1f;
        return ev.trafficMultiplier;
    }

    static WeatherEventData PickWeighted(WeatherEventData[] events)
    {
        float totalWeight = 0f;
        foreach (var e in events) totalWeight += e.chanceWeight;
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var e in events)
        {
            cumulative += e.chanceWeight;
            if (roll <= cumulative) return e;
        }
        return events[events.Length - 1];
    }
}