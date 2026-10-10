using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayManager : MonoBehaviour
{
    public GameClock clock;
    public GameBalanceConfig config;
    public ItemData[] allItems; // all 3 categories - used to initialize stock at game start
    public LotData currentLotData; // set by StartGame(), not dragged in Inspector anymore
    public WeatherEventData[] possibleWeatherEvents;
    public TheftEventData theftEvent;
    public GameObject thiefPrefab; // fixed reference, same asset every store - wire directly in Inspector
    public UIManager uiManager;
    public NotificationUI notificationUI;
    public CustomerSpawner customerSpawner; // set by StartGame(), comes from the instantiated store
    public TutorialManager tutorialManager;

    public StoreState store = new StoreState();
    public int currentDay = 1;
    DayResults results;
    bool gameStarted;
    bool weatherNotified;
    bool isGameOver; // Update: true once the complaint threshold is hit - freezes the day loop entirely

    Transform theftSpawnPoint;
    Transform theftExitPoint;

    void Awake()
    {
        clock.OnHourTick += HandleHourTick;
    }

    // Call this exactly once, right after the player's store is instantiated and their
    // first lot is confirmed (see GameplaySceneBootstrapper below). Do NOT call BeginDay()
    // directly from anywhere else - this is the only valid entry point into the day loop.
    // Update: now also takes the store's theft spawn/exit points, found by GameplaySceneBootstrapper
    // the same way CustomerSpawner was, so DayManager can actually spawn the Thief when it triggers.
    public void StartGame(LotSite lot, LotData lotData, CustomerSpawner spawner, Transform theftSpawn, Transform theftExit)
    {
        if (gameStarted) return; // safety guard against being called twice
        gameStarted = true;

        store.cash = config.startingCash;
        store.lot = lot;
        currentLotData = lotData;
        customerSpawner = spawner;
        theftSpawnPoint = theftSpawn;
        theftExitPoint = theftExit;
        store.baseCustomersToday = Mathf.RoundToInt(config.baseCustomersPerDay); // Day 1 starting point - grows from here each day

        // Update: Day 1 now starts with real stock (clamped to the capacity cap) instead of
        // empty shelves - previously every item started at 0, meaning zero sales were possible
        // until the first restock arrived the next morning.
        int day1Stock = Mathf.Min(config.day1StartingStockPerItem, store.upgrades.MaxStockPerItem(config));
        foreach (var item in allItems)
            store.currentStock[item] = day1Stock;

        BeginDay();
        tutorialManager.TryAdvance(TutorialTrigger.OnGameStart);
    }

    void BeginDay()
    {
        EconomyManager.ResolveDelivery(store, config);
        results = new DayResults();
        clock.ResetForNewDay();

        // Update: defensive null-check - a missing NotificationUI reference used to crash this
        // entire method (and everything calling it, including GameplaySceneBootstrapper.Start()),
        // silently aborting the camera-snap and everything else that runs after StartGame().
        if (notificationUI != null) notificationUI.HideAll();
        else Debug.LogWarning("DayManager's Notification UI field isn't wired - weather/theft banners won't show.");

        weatherNotified = false;

        // Update: random events (weather + theft) are gated to Day 2 onward - Day 1 always stays calm.
        if (currentDay >= 2)
        {
            results.triggeredEvent = EventManager.RollWeatherEvent(store.lot, currentLotData, possibleWeatherEvents, /*baseChance*/ 0.3f);
            if (results.triggeredEvent != null)
                results.eventTriggerHour = Random.Range(results.triggeredEvent.earliestTriggerHour, Mathf.Min(results.triggeredEvent.latestTriggerHour, 12));

            results.theftOccurred = EventManager.RollTheftEvent(store.lot, currentLotData, theftEvent);
            if (results.theftOccurred)
                results.theftTriggerHour = Random.Range(theftEvent.earliestTriggerHour, theftEvent.latestTriggerHour);
        }
    }

    // Update: no longer resolves every hour's sales the same way. The CLOSING tick (hour >=
    // closeHour) now resolves instantly, same as before the gradual rewrite - only REGULAR open
    // hours get spread across real time via the coroutine below.
    void HandleHourTick(int hour)
    {
        if (!gameStarted || isGameOver) return; // ignore ticks before a store exists, or after the game has ended

        // Update: fire the weather notification exactly once, the hour it actually starts affecting traffic.
        if (results.triggeredEvent != null && !weatherNotified && hour >= results.eventTriggerHour)
        {
            NotificationEvents.Raise($"{results.triggeredEvent.eventName}! Customers x{results.triggeredEvent.trafficMultiplier:F1}");
            weatherNotified = true;
        }

        // Update: theft now triggers at its own specific hour, spawning the real Thief object.
        if (results.theftOccurred && !results.theftResolved && hour == results.theftTriggerHour)
        {
            TriggerTheft();
        }

        float multiplier = EventManager.GetCurrentMultiplier(results.triggeredEvent, results.eventTriggerHour, hour);
        int totalOpenHours = config.closeHour - config.openHour;
        int customersThisHour = EconomyManager.CustomersForHour(store, currentLotData, config, multiplier, totalOpenHours);

        // Update: BUG FIX - the closing tick used to also go through the gradual coroutine,
        // which meant EndDay() (and the closing screen) didn't actually fire until ~17 real
        // seconds AFTER the clock's displayed time had already frozen at closeHour - during
        // that gap, sales kept resolving and the game visibly "kept running" with a stopped
        // clock. Resolving this specific tick instantly, exactly like before the gradual
        // rewrite, keeps "clock freezes" and "day actually ends" perfectly in sync again.
        if (hour >= config.closeHour)
        {
            EconomyManager.ResolveHourOfSales(store, results, customersThisHour, config, currentDay);

            if (results.complaints >= config.maxComplaintsPerDay)
            {
                TriggerGameOver();
                return;
            }

            EndDay();
            return;
        }

        StartCoroutine(ResolveHourGradually(customersThisHour, hour));
    }

    // Update: simplified - no longer needs to check/call EndDay() at all, since the closing
    // tick is now handled entirely by the instant path in HandleHourTick above. This coroutine
    // only ever runs for regular open hours (hour < closeHour).
    IEnumerator ResolveHourGradually(int customerCount, int hour)
    {
        if (customerCount <= 0) yield break; // nothing to spread out this hour

        // How many real seconds this in-game hour takes, at the clock's current speed -
        // snapshotted once at the start of the hour (a mid-hour speed change won't retroactively
        // reflow the remaining delay, which is an acceptable simplification here).
        float realSecondsThisHour = 60f / (clock.MinutesPerRealSecond * Mathf.Max(clock.speedMultiplier, 0.0001f));
        float delayBetweenCustomers = realSecondsThisHour / customerCount;

        int maxVisual = config.maxVisibleCustomersPerHour;
        int visualsSpawnedSoFar = 0;

        for (int i = 0; i < customerCount; i++)
        {
            EconomyManager.ResolveSingleCustomer(store, results, config, currentDay);

            // Spread the (capped) visible customer spawns proportionally across the same
            // real-time window, instead of spawning all of them in one instant burst either.
            if (visualsSpawnedSoFar < maxVisual && (i * maxVisual / customerCount) >= visualsSpawnedSoFar)
            {
                customerSpawner.SpawnForHour(1);
                visualsSpawnedSoFar++;
            }

            // Update: losing condition - checked after EVERY customer now, not just once at the
            // end of the hour, so hitting the threshold stops things immediately, mid-hour.
            if (results.complaints >= config.maxComplaintsPerDay)
            {
                TriggerGameOver();
                yield break;
            }

            yield return new WaitForSeconds(delayBetweenCustomers);
        }
    }

    // Update: freezes the clock and shows the "Store Closed" screen - the game does not
    // resume from this state, matching a permanent loss condition rather than a warning.
    void TriggerGameOver()
    {
        isGameOver = true;
        clock.speedMultiplier = 0f;
        uiManager.ShowGameOver();
    }

    // Update: shows the "THEFT!" notification and actually instantiates the Thief prefab at the
    // store's spawn/exit points - previously theft only existed as a silent bool with no in-scene presence.
    void TriggerTheft()
    {
        NotificationEvents.Raise("THEFT! Go to First Person and catch the thief!");

        if (thiefPrefab != null && theftSpawnPoint != null && theftExitPoint != null)
        {
            var thiefObj = Instantiate(thiefPrefab);
            var thief = thiefObj.GetComponent<Thief>();
            thief.dayManager = this;
            thief.Initialize(theftSpawnPoint, theftExitPoint);
        }
        else
        {
            Debug.LogWarning("Theft triggered but thiefPrefab/theftSpawnPoint/theftExitPoint isn't wired - nothing was spawned.");
        }
    }

    // Note: this fires every day the recap screen is shown, since BeginDay()/HandleHourTick()
    // run in a continuous loop for as long as gameStarted is true - each new day naturally
    // reaches config.closeHour again after clock.ResetForNewDay(), so EndDay() (and therefore
    // the recap) is guaranteed to fire once per day automatically, with no extra code needed.
    void EndDay()
    {
        uiManager.ShowClosingSummary(store, results);
        tutorialManager.TryAdvance(TutorialTrigger.OnFirstDayRecap);
        // wait for player to confirm restock (or skip) via ShoppingUI, then call AdvanceDay()
    }

    // Update: guarded the same way HandleHourTick already was - if StartGame() never ran
    // (store was never placed), this now fails loudly instead of cascading into the
    // RollWeatherEvent null-reference crash.
    public void AdvanceDay()
    {
        if (!gameStarted)
        {
            Debug.LogError("AdvanceDay() called before StartGame() ran - the store was never placed. Restart from MapPlacement.");
            return;
        }
        if (isGameOver) return; // Update: game has already ended - nothing should be able to advance past this
        currentDay++;
        store.baseCustomersToday += Random.Range(5, 11); // Random.Range(int,int) max is EXCLUSIVE, so this gives 5-10 inclusive
        BeginDay();
    }

    // Called by Thief.cs when the theft minigame resolves (see section 6)
    public void NotifyTheftResult(bool caught)
    {
        results.theftCaught = caught;
        results.theftResolved = true;
        // Update: HideTheftBanner() call removed entirely - toasts auto-dismiss on their own
        // timer now, there's nothing left to manually hide when theft resolves.

        if (!caught)
            StealItems();
    }

    // Update: escape penalty. Removes a random total of min..max units, one unit at a time from a
    // randomly chosen category that is unlocked today AND still has stock. This is what makes the loss
    // land on a random subset of categories (it could hit just one, or all of them). Stops early if
    // everything unlocked runs out. The old version took a flat amount from EVERY item, including
    // ones not unlocked yet, and never refreshed the Inventory panel or told the player what was lost.
    void StealItems()
    {
        var candidates = new System.Collections.Generic.List<ItemData>();
        foreach (var item in allItems)
            if (item.unlockOnDay <= currentDay && store.currentStock.GetValueOrDefault(item) > 0)
                candidates.Add(item);

        if (candidates.Count == 0) return;

        int toSteal = Random.Range(theftEvent.minItemsLostIfEscaped, theftEvent.maxItemsLostIfEscaped + 1); // int max is EXCLUSIVE
        var lost = new System.Collections.Generic.Dictionary<ItemData, int>();
        int stolen = 0;

        while (stolen < toSteal && candidates.Count > 0)
        {
            int i = Random.Range(0, candidates.Count);
            ItemData pick = candidates[i];

            store.currentStock[pick]--;
            lost[pick] = lost.GetValueOrDefault(pick) + 1;
            stolen++;

            if (store.currentStock[pick] <= 0) candidates.RemoveAt(i); // emptied, can't steal more of it
        }

        StockEvents.Raise(); // refresh the Inventory panel if it's open

        var parts = new System.Collections.Generic.List<string>();
        foreach (var kv in lost) parts.Add($"{kv.Value} {kv.Key.itemName}");
        NotificationEvents.Raise($"The thief escaped with {stolen} items ({string.Join(", ", parts)})!");
    }

    // Update: called by FirstPersonController when the player shoots a customer. Each shot adds
    // config.complaintsPerCustomerShot complaints and uses the same game-over check as normal sales.
    public void RegisterCustomerShot()
    {
        if (!gameStarted || isGameOver) return;

        results.complaints += config.complaintsPerCustomerShot;
        NotificationEvents.Raise("You shot a customer! Complaints are piling up.");

        if (results.complaints >= config.maxComplaintsPerDay)
            TriggerGameOver();
    }

    // Update: baked directly into the class now - this was previously only given as a separate
    // "add this to your file" instruction in the Lighting section (Section 11) and never actually
    // included here, which is easy to miss. LightingManager calls this every frame to know
    // whether today counts as a rain day, regardless of whether the event has actually triggered
    // yet (the gloomy sky mood starts at morning, the traffic-reducing effect starts later).
    public bool CurrentDayHasRainEvent()
    {
        return results != null && results.triggeredEvent != null && results.triggeredEvent.isRainType;
    }
}