using UnityEngine;

[System.Serializable]
public struct TimeOfDayKeyframe
{
    public float hour; // 0-24
    public Color color;
    [Range(0f, 3f)] public float intensity;
}

public class LightingManager : MonoBehaviour
{
    public Light sunLight; // the Directional Light - Shadow Type set to "No Shadows" in the Editor
    public GameClock clock;
    public DayManager dayManager;

    [Header("Time-of-day keyframes, sorted by hour (0-24), wraps across midnight")]
    public TimeOfDayKeyframe[] keyframes = new TimeOfDayKeyframe[]
    {
        new TimeOfDayKeyframe { hour = 6f,  color = new Color(1f, 0.75f, 0.55f), intensity = 0.8f }, // morning - soft warm
        new TimeOfDayKeyframe { hour = 12f, color = new Color(1f, 0.98f, 0.9f),  intensity = 1.3f }, // noon - bright, near-white
        new TimeOfDayKeyframe { hour = 16f, color = new Color(1f, 0.8f, 0.5f),   intensity = 1.0f }, // afternoon - warm golden
        new TimeOfDayKeyframe { hour = 20f, color = new Color(0.6f, 0.4f, 0.6f), intensity = 0.5f }  // evening - dim purple/orange
    };

    [Header("Gloomy override - applied ALL DAY (from morning) whenever today's weather event is a rain tier")]
    public Color gloomyTint = new Color(0.55f, 0.55f, 0.6f);
    [Range(0f, 1f)] public float gloomyBlend = 0.6f;
    public float gloomyIntensityMultiplier = 0.5f;

    void Update()
    {
        if (clock == null || sunLight == null) return;

        float currentHour = clock.currentHour + (clock.currentMinute / 60f);
        var (baseColor, baseIntensity) = SampleKeyframes(currentHour);

        // Update: gloomy applies for the WHOLE day the moment it's a rain day, not just from
        // the event's actual trigger hour - the sky looks gloomy from morning even though the
        // traffic-reducing effect itself only kicks in later, matching the requested mood.
        if (dayManager != null && dayManager.CurrentDayHasRainEvent())
        {
            baseColor = Color.Lerp(baseColor, gloomyTint, gloomyBlend);
            baseIntensity *= gloomyIntensityMultiplier;
        }

        sunLight.color = baseColor;
        sunLight.intensity = baseIntensity;
    }

    (Color, float) SampleKeyframes(float hour)
    {
        if (keyframes.Length == 0) return (Color.white, 1f);
        if (keyframes.Length == 1) return (keyframes[0].color, keyframes[0].intensity);

        for (int i = 0; i < keyframes.Length; i++)
        {
            int next = (i + 1) % keyframes.Length;
            float h0 = keyframes[i].hour;
            float h1 = keyframes[next].hour;

            bool wraps = h1 <= h0; // e.g. evening(20) -> morning(6) wraps past midnight
            bool inRange = wraps ? (hour >= h0 || hour < h1) : (hour >= h0 && hour < h1);
            if (!inRange) continue;

            float span = wraps ? (24f - h0 + h1) : (h1 - h0);
            float progress = wraps ? ((hour >= h0 ? hour - h0 : hour + 24f - h0) / span) : ((hour - h0) / span);

            Color c = Color.Lerp(keyframes[i].color, keyframes[next].color, progress);
            float inten = Mathf.Lerp(keyframes[i].intensity, keyframes[next].intensity, progress);
            return (c, inten);
        }

        return (keyframes[0].color, keyframes[0].intensity); // fallback, shouldn't normally hit
    }
}