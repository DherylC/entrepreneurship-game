using UnityEngine;

[CreateAssetMenu(fileName = "WeatherEventData", menuName = "Config/WeatherEventData")]
public class WeatherEventData : ScriptableObject
{
    public string eventName;
    public float trafficMultiplier = 1f; // applies only from trigger hour onward
    public float chanceWeight = 0f;      // for weighted random selection among tiers
    public int earliestTriggerHour = 8;
    public int latestTriggerHour = 12;   // must stay before noon
}