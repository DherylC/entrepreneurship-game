using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryRowUI : MonoBehaviour
{
    public TMP_Text itemNameText;
    public TMP_Text countOrStatusText;

    ItemData item; // Update: kept so RefreshStock() can recompute later without needing the item passed in again

    // No currentDay/locked handling needed here anymore - InventoryUI already filters
    // out locked items before this ever gets called, so every row is guaranteed unlocked.
    public void Initialize(ItemData itemData, StoreState store)
    {
        item = itemData;
        itemNameText.text = item.itemName;
        RefreshStock(store);
    }

    // Update: NEW - just recomputes the count/status text, called repeatedly by InventoryUI's
    // live StockEvents subscription without touching the name text or recreating anything.
    public void RefreshStock(StoreState store)
    {
        int count = store.currentStock.GetValueOrDefault(item);
        countOrStatusText.text = count > 0 ? $"{count} Items - In stock" : "0 Items - Out of Stock";
    }
}