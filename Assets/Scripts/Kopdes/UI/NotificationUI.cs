using UnityEngine;
using System.Collections.Generic;

public class NotificationUI : MonoBehaviour
{
    public Transform toastContainer; // has a Vertical Layout Group - this is what makes the stack re-flow automatically
    public GameObject toastPrefab;

    List<GameObject> activeToasts = new();

    void OnEnable() => NotificationEvents.OnNotify += ShowNotification;
    void OnDisable() => NotificationEvents.OnNotify -= ShowNotification;

    public void ShowNotification(string message)
    {
        var toastObj = Instantiate(toastPrefab, toastContainer);
        toastObj.transform.SetSiblingIndex(0); // newest appears at the top of the stack
        activeToasts.Add(toastObj);
        toastObj.GetComponent<NotificationToastUI>().Show(message, () => activeToasts.Remove(toastObj));
    }

    // Kept with their old names so DayManager's existing ShowWeatherBanner()/ShowTheftBanner()
    // calls don't need to change - both just route into the same toast system now.
    public void ShowWeatherBanner(WeatherEventData ev) => ShowNotification($"{ev.eventName}! Customers x{ev.trafficMultiplier:F1}");
    public void ShowTheftBanner() => ShowNotification("THEFT! Go to First Person and catch the thief!");

    // Update: toasts now auto-dismiss on their own timer, so there's nothing left to manually
    // hide once shown - these are kept as no-ops purely so DayManager's existing calls to them
    // (e.g. NotifyTheftResult's HideTheftBanner()) don't need to be removed or edited.
    public void HideWeatherBanner() { }
    public void HideTheftBanner() { }

    // Immediately clears every currently-visible toast - DayManager.BeginDay() still calls this
    // at the start of each day as a hard reset, in case anything from yesterday is still animating.
    public void HideAll()
    {
        foreach (var toast in activeToasts) if (toast != null) Destroy(toast);
        activeToasts.Clear();
    }
}