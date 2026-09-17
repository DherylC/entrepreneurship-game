using UnityEngine;
using TMPro;

public class NotificationUI : MonoBehaviour
{
    public GameObject weatherBannerRoot;
    public TMP_Text weatherBannerText;
    public GameObject theftBannerRoot;
    public TMP_Text theftBannerText;

    public void ShowWeatherBanner(WeatherEventData ev)
    {
        weatherBannerRoot.SetActive(true);
        weatherBannerText.text = $"{ev.eventName}, customers x{ev.trafficMultiplier:F1}";
    }

    public void HideWeatherBanner() => weatherBannerRoot.SetActive(false);

    public void ShowTheftBanner()
    {
        theftBannerRoot.SetActive(true);
        theftBannerText.text = "THEFT! Go to First Person and catch the thief!";
    }

    public void HideTheftBanner() => theftBannerRoot.SetActive(false);

    // Called once at the start of every new day (DayManager.BeginDay) to clear
    // yesterday's banners before today's events get a chance to roll.
    public void HideAll()
    {
        HideWeatherBanner();
        HideTheftBanner();
    }
}