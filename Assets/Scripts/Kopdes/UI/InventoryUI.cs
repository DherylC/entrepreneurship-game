using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public Transform rowContainer;
    public GameObject rowPrefab;
    public DayManager dayManager;

    List<InventoryRowUI> activeRows = new();

    public void OpenPanel()
    {
        BuildRows();
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