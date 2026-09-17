using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ClosingSummaryUI : MonoBehaviour
{
    public Transform rowContainer;
    public GameObject cardPrefab;
    public TMP_Text totalGainsText;
    public TMP_Text weatherEventText;
    public TMP_Text theftEventText;
    public DayManager dayManager;

    public void Populate(StoreState store, DayResults results)
    {
        foreach (Transform child in rowContainer) Destroy(child.gameObject);

        totalGainsText.text = $"Rp {results.revenue:F0}";

        weatherEventText.gameObject.SetActive(results.triggeredEvent != null);
        if (results.triggeredEvent != null)
            weatherEventText.text = $"{results.triggeredEvent.eventName} - starting {results.eventTriggerHour}:00";

        theftEventText.gameObject.SetActive(results.theftOccurred);
        if (results.theftOccurred)
            theftEventText.text = results.theftCaught ? "Theft - Caught" : "Theft - Items lost";

        foreach (var kvp in results.itemsSold)
        {
            var cardObj = Instantiate(cardPrefab, rowContainer);
            var card = cardObj.GetComponent<RecapItemCardUI>();
            int remaining = store.currentStock.GetValueOrDefault(kvp.Key);
            card.Initialize(kvp.Key, kvp.Value, remaining);
        }
    }

    public void OnNextClicked()
    {
        dayManager.AdvanceDay();
        // TODO: route back to UIManager.ShowShopping() here if restock should immediately follow, per your mockup's flow
    }
}