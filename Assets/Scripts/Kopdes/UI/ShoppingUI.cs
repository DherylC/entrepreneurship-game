using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShoppingUI : MonoBehaviour
{
    public Transform rowContainer;   // empty object with a Vertical Layout Group
    public GameObject rowPrefab;     // the RestockRowUI prefab built per the step-by-step guide
    public TMP_Text totalCostText;
    public DayManager dayManager;

    List<RestockRowUI> activeRows = new();

    public void OpenPanel()
    {
        BuildRows();
    }

    void BuildRows()
    {
        // Clear any old rows first (in case this panel gets reopened on a later day)
        foreach (Transform child in rowContainer) Destroy(child.gameObject);
        activeRows.Clear();

        foreach (ItemData item in dayManager.allItems)
        {
            // No skipping here anymore - every item gets a row, RestockRowUI itself
            // decides whether to show it as interactive or Locked based on currentDay.
            var rowObj = Instantiate(rowPrefab, rowContainer);
            var row = rowObj.GetComponent<RestockRowUI>();
            row.Initialize(item, dayManager.config, dayManager.store, RecalculateTotal, dayManager.currentDay);
            activeRows.Add(row);
        }

        RecalculateTotal();
    }

    void RecalculateTotal()
    {
        float total = dayManager.config.flatDeliveryFee;
        foreach (var row in activeRows)
            total += row.GetItem().flatRestockCost * row.GetQuantity();
        totalCostText.text = total.ToString("F0");
    }

    // Update: now checks closing hour (defensive - Shopping should normally only be reachable
    // via the closing recap flow anyway, but this guards against any future path that opens it
    // early) and relies on PlaceOrder's own cash check instead of duplicating that math here.
    public void ConfirmOrder()
    {
        if (dayManager.clock.currentHour < dayManager.config.closeHour)
        {
            Debug.Log("Can't restock yet - store is still open.");
            return; // TODO: show a real on-screen message instead of just a log
        }

        var order = new Dictionary<ItemData, int>();
        foreach (var row in activeRows)
            if (row.GetQuantity() > 0) order[row.GetItem()] = row.GetQuantity();

        if (order.Count == 0)
        {
            dayManager.AdvanceDay(); // nothing ordered - proceed like a skip
            return;
        }

        bool success = EconomyManager.PlaceOrder(dayManager.store, order, dayManager.config);
        if (!success)
        {
            Debug.Log("Not enough cash for this order.");
            return; // TODO: show a real on-screen "insufficient funds" message
        }

        dayManager.AdvanceDay();
    }

    public void SkipOrder()
    {
        if (dayManager.clock.currentHour < dayManager.config.closeHour)
        {
            Debug.Log("Can't skip restock yet - store is still open.");
            return;
        }

        dayManager.store.queuedOrder = null;
        dayManager.AdvanceDay();
    }
}