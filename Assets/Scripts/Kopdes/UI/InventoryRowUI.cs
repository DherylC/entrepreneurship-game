using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryRowUI : MonoBehaviour
{
    public TMP_Text itemNameText;
    public TMP_Text countOrStatusText;

    // No currentDay/locked handling needed here anymore - InventoryUI already filters
    // out locked items before this ever gets called, so every row is guaranteed unlocked.
    public void Initialize(ItemData item, StoreState store)
    {
        itemNameText.text = item.itemName;

        int count = store.currentStock.GetValueOrDefault(item);
        countOrStatusText.text = count > 0 ? $"{count} Items - In stock" : "0 Items - Out of Stock";
    }
}