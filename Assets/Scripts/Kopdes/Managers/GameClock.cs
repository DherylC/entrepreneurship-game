using UnityEngine;

public class GameClock : MonoBehaviour
{
    public int currentHour = 8;
    public int currentMinute = 0;
    public GameBalanceConfig config;
    public float speedMultiplier = 1f;

    public event System.Action<int> OnHourTick;

    float accumulatedMinutes;
    // Update: made public - DayManager needs this to know how long one in-game hour actually
    // takes in real time, so it can spread that hour's sales evenly across it instead of
    // resolving them all in one instant burst.
    public float MinutesPerRealSecond => (config.closeHour - config.openHour) * 60f / config.realSecondsPerDay;

    void Update()
    {
        if (currentHour >= config.closeHour) return;

        accumulatedMinutes += MinutesPerRealSecond * speedMultiplier * Time.deltaTime;
        while (accumulatedMinutes >= 1f)
        {
            accumulatedMinutes -= 1f;
            AdvanceOneMinute();
        }
    }

    void AdvanceOneMinute()
    {
        currentMinute++;
        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;
            OnHourTick?.Invoke(currentHour);
        }
    }

    public void ToggleSpeed() => speedMultiplier = (speedMultiplier == 1f) ? 2f : 1f;

    public void ResetForNewDay()
    {
        currentHour = config.openHour;
        currentMinute = 0;
    }
}