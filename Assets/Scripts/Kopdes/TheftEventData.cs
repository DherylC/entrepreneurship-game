using UnityEngine;

[CreateAssetMenu(fileName = "TheftEventData", menuName = "Config/TheftEventData")]
public class TheftEventData : ScriptableObject
{
    public float dailyChance = 0f;
    public int itemsLostIfEscaped = 0;

    // Update: theft now happens at a specific hour within the day, same concept as
    // WeatherEventData's trigger window - this is what makes it a real "ongoing" event
    // that can be notified about, rather than a value resolved silently at day start.
    public int earliestTriggerHour = 8;
    public int latestTriggerHour = 20;
}