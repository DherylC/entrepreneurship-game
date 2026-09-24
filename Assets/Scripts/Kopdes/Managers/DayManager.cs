using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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

    // Update: no longer resolves sales directly - now just handles the once-per-hour checks
    // (weather/theft notifications) and kicks off a coroutine that spreads this hour's customers
    // across the actual real-time duration of the hour, instead of resolving them all at once.
    void HandleHourTick(int hour)
    {
        if (!gameStarted || isGameOver) return; // ignore ticks before a store exists, or after the game has ended

        // Update: fire the weather notification exactly once, the hour it actually starts affecting traffic.
        if (results.triggeredEvent != null && !weatherNotified && hour >= results.eventTriggerHour)
        {
            if (notificationUI != null) notificationUI.ShowWeatherBanner(results.triggeredEvent);
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

        StartCoroutine(ResolveHourGradually(customersThisHour, hour));
    }

    // Update: NEW - spreads one hour's worth of customers evenly across however long that
    // in-game hour actually takes in real time (accounting for the current clock speed), instead
    // of resolving them all in a single instant burst. This is what makes cash/popups trickle in
    // steadily rather than arriving in one lump sum once per hour.
    IEnumerator ResolveHourGradually(int customerCount, int hour)
    {
        if (customerCount <= 0)
        {
            if (hour >= config.closeHour) EndDay();
            yield break;
        }

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

        if (hour >= config.closeHour) EndDay();
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
        if (notificationUI != null) notificationUI.ShowTheftBanner();

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
        if (notificationUI != null) notificationUI.HideTheftBanner();

        if (!caught)
        {
            foreach (var item in allItems)
            {
                int loss = Mathf.Min(store.currentStock.GetValueOrDefault(item), theftEvent.itemsLostIfEscaped);
                store.currentStock[item] -= loss;
            }
        }
    }
    public bool CurrentDayHasRainEvent()
    {
        return results != null && results.triggeredEvent != null && results.triggeredEvent.isRainType;
    }
}