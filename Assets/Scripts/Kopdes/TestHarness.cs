using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TestHarness : MonoBehaviour
{
    [Header("References to real systems (same objects the real game uses)")]
    public DayManager dayManager;
    public GameClock clock;
    public GameBalanceConfig config;
    public ItemData[] allItems;

    [Header("Debug display")]
    public TMP_Text debugText;
    // No panelRoot / key-toggle logic - toggle the DebugPanel object's active checkbox
    // directly in the Hierarchy/Inspector whenever you want it shown or hidden.

    int[] orderQuantities;

    void Awake()
    {
        orderQuantities = new int[allItems.Length];
    }

    void Update()
    {
        // Unity automatically skips Update() on inactive GameObjects, so this only
        // runs while DebugPanel is actually checked on in the Hierarchy - no extra
        // active/inactive logic needed here at all.
        RefreshDebugText();
    }

    // ---- Cash / stock ----

    public void Test_AddCash() { dayManager.store.cash += 100000; Log("Added 100,000 cash"); }

    public void Test_FillAllStock()
    {
        foreach (var item in allItems) dayManager.store.currentStock[item] = 999;
        Log("Filled all stock to 999");
    }

    public void Test_EmptyAllStock()
    {
        foreach (var item in allItems) dayManager.store.currentStock[item] = 0;
        Log("Emptied all stock");
    }

    // ---- Clock ----

    public void Test_ToggleClockSpeed() { clock.ToggleSpeed(); Log($"Clock speed is now {clock.speedMultiplier}x"); }
    public void Test_PauseClock() { clock.speedMultiplier = 0f; Log("Clock paused"); }
    public void Test_ResumeClock() { clock.speedMultiplier = 1f; Log("Clock resumed at 1x"); }

    public void Test_SkipToClosing()
    {
        // Jumps the displayed time only - does NOT resolve sales for skipped hours.
        clock.currentHour = config.closeHour;
        clock.currentMinute = 0;
        Log($"Jumped clock to {config.closeHour}:00 (no sales resolved for skipped hours)");
    }

    // ---- Day ----

    public void Test_ForceAdvanceDay() { dayManager.AdvanceDay(); Log($"Advanced to Day {dayManager.currentDay}"); }

    public void Test_SimulateOneHour()
    {
        int fakeCustomers = 20;
        var results = new DayResults();
        EconomyManager.ResolveHourOfSales(dayManager.store, results, fakeCustomers, config, dayManager.currentDay);
        Log($"Simulated hour: {results.revenue:F0} revenue, {results.complaints} complaints");
    }

    // ---- Upgrades ----

    public void Test_UpgradeCapacity() { Log(UpgradeManager.TryUpgrade(dayManager.store, config, UpgradeTrack.Capacity) ? "Capacity upgraded!" : "Failed (cash/level)"); }
    public void Test_UpgradeDecor() { Log(UpgradeManager.TryUpgrade(dayManager.store, config, UpgradeTrack.Decor) ? "Decor upgraded!" : "Failed (cash/level)"); }
    public void Test_UpgradeFacility() { Log(UpgradeManager.TryUpgrade(dayManager.store, config, UpgradeTrack.Facility) ? "Facility upgraded!" : "Failed (cash/level)"); }

    // ---- Restock / order draft (mirrors the real ShoppingUI flow) ----

    public void Test_IncrementOrder(int itemIndex) { orderQuantities[itemIndex] += 10; Log($"Order draft: {allItems[itemIndex].itemName} = {orderQuantities[itemIndex]}"); }
    public void Test_DecrementOrder(int itemIndex) { orderQuantities[itemIndex] = Mathf.Max(0, orderQuantities[itemIndex] - 10); Log($"Order draft: {allItems[itemIndex].itemName} = {orderQuantities[itemIndex]}"); }

    public float Test_GetOrderTotalCost()
    {
        float total = config.flatDeliveryFee;
        for (int i = 0; i < allItems.Length; i++) total += allItems[i].flatRestockCost * orderQuantities[i];
        return total;
    }

    // Update: simplified now that PlaceOrder self-validates and returns bool -
    // no need to manually check cash here anymore, one less place that math can drift out of sync.
    public void Test_ConfirmOrder()
    {
        var order = new Dictionary<ItemData, int>();
        for (int i = 0; i < allItems.Length; i++)
            if (orderQuantities[i] > 0) order[allItems[i]] = orderQuantities[i];

        if (order.Count == 0) { Log("Order is empty - nothing to confirm."); return; }

        bool success = EconomyManager.PlaceOrder(dayManager.store, order, config);
        if (!success) { Log("Not enough cash for this order."); return; }

        System.Array.Clear(orderQuantities, 0, orderQuantities.Length);
        Log("Order confirmed. Arrives next morning.");
    }

    public void Test_SkipOrder()
    {
        dayManager.store.queuedOrder = null;
        System.Array.Clear(orderQuantities, 0, orderQuantities.Length);
        Log("Skipped buy phase.");
    }

    // ---- Helpers ----

    void Log(string message) { Debug.Log(message); if (debugText != null) debugText.text = message + "\n\n" + BuildStateSnapshot(); }
    void RefreshDebugText() { if (debugText != null) debugText.text = BuildStateSnapshot(); }

    string BuildStateSnapshot()
    {
        string s = $"Day: {dayManager.currentDay}\nTime: {clock.currentHour:00}:{clock.currentMinute:00}\nClock speed: {clock.speedMultiplier}x\n";
        s += $"Cash: {dayManager.store.cash:F0}\n";
        s += $"Base customers today: {dayManager.store.baseCustomersToday} (grows +5-10/day)\n";
        s += $"Capacity Lv: {dayManager.store.upgrades.capacityLevel}  Decor Lv: {dayManager.store.upgrades.decorLevel}  Facility Lv: {dayManager.store.upgrades.facilityLevel}\n\nStock:\n";
        foreach (var item in allItems) s += $"  {item.itemName}: {dayManager.store.currentStock.GetValueOrDefault(item)}\n";
        s += "\nOrder draft:\n";
        for (int i = 0; i < allItems.Length; i++) if (orderQuantities[i] > 0) s += $"  {allItems[i].itemName}: {orderQuantities[i]}\n";
        s += $"  Total (incl. {config.flatDeliveryFee:F0} shipping): {Test_GetOrderTotalCost():F0}\n";
        return s;
    }
}