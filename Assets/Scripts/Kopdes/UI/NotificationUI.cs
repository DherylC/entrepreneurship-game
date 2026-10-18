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
        toastObj.SetActive(true); // defensive - guarantees the clone is active even if the prefab source ever ends up inactive
        toastObj.transform.SetSiblingIndex(0); // newest appears at the top of the stack

        // Update: forces the layout group to recalculate immediately instead of waiting for
        // Unity's next natural layout pass - this alone doesn't fix the animation conflict (see
        // NotificationToastUI below for that), but it does make sure the toast's SLOT position
        // is correct from the very first frame, rather than momentarily sitting wherever the
        // prefab was authored until something else happens to trigger a rebuild.
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(toastContainer.GetComponent<RectTransform>());

        activeToasts.Add(toastObj);
        toastObj.GetComponent<NotificationToastUI>().Show(message, () => activeToasts.Remove(toastObj));
    }

    // Update: ShowWeatherBanner/ShowTheftBanner/HideWeatherBanner/HideTheftBanner REMOVED.
    // These used to be called directly through DayManager's own `notificationUI` field reference -
    // a second pathway completely separate from NotificationEvents (which stock alerts already
    // used). Two different ways to show a notification meant two different ways for wiring to
    // silently break. DayManager now calls NotificationEvents.Raise(...) directly for weather/
    // theft too, exactly like every other notification - one single pathway, no exceptions.

    // Immediately clears every currently-visible toast - DayManager.BeginDay() still calls this
    // at the start of each day as a hard reset, in case anything from yesterday is still animating.
    // This is the ONE remaining place that still needs a direct object reference, since
    // NotificationEvents is fire-and-forget with no natural "clear everything" signal.
    public void HideAll()
    {
        foreach (var toast in activeToasts) if (toast != null) Destroy(toast);
        activeToasts.Clear();
    }
}