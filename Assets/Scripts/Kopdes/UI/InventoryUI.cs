using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public Transform rowContainer;
    public GameObject rowPrefab;
    public DayManager dayManager;

    List<InventoryRowUI> activeRows = new();

    // Update: subscribes only while the panel is actually active (Unity calls OnEnable/OnDisable
    // automatically whenever UIManager does SetActive(true/false) on this object) - so this never
    // does wasted work while some other panel is open, and never leaks a subscription either.
    void OnEnable() => StockEvents.OnStockChanged += HandleStockChanged;
    void OnDisable() => StockEvents.OnStockChanged -= HandleStockChanged;

    public void OpenPanel()
    {
        BuildRows();
    }

    // Update: lightweight refresh - updates each existing row's numbers in place instead of
    // destroying and re-instantiating the whole list every time a single item sells.
    void HandleStockChanged()
    {
        foreach (var row in activeRows)
            row.RefreshStock(dayManager.store);
    }

    void BuildRows()
    {
        foreach (Transform child in rowContainer) Destroy(child.gameObject);
        activeRows.Clear();

        foreach (ItemData item in dayManager.allItems)
        {
            if (item.unlockOnDay > dayManager.currentDay) continue; // skip locked items entirely - Inventory only ever shows what's actually available

            var rowObj = Instantiate(rowPrefab, rowContainer);
            var row = rowObj.GetComponent<InventoryRowUI>();
            row.Initialize(item, dayManager.store);
            activeRows.Add(row);
        }
    }
}