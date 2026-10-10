using UnityEngine;

[CreateAssetMenu(fileName = "TheftEventData", menuName = "Config/TheftEventData")]
public class TheftEventData : ScriptableObject
{
    public float dailyChance = 50f;

    // Update: replaces the old fixed itemsLostIfEscaped. If the thief escapes, a random TOTAL between
    // these two values is removed, spread across randomly picked unlocked categories (see DayManager.StealItems).
    public int minItemsLostIfEscaped = 8;
    public int maxItemsLostIfEscaped = 20;

    // Update: theft now happens at a specific hour within the day, same concept as
    // WeatherEventData's trigger window - this is what makes it a real "ongoing" event
    // that can be notified about, rather than a value resolved silently at day start.
    public int earliestTriggerHour = 10;
    public int latestTriggerHour = 14;
}