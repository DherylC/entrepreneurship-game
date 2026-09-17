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

        foreach (var item in allItems)
            store.currentStock[item] = 0; // every item starts empty until the player's first restock

        BeginDay();
        tutorialManager.TryAdvance(TutorialTrigger.OnGameStart);
    }

    void BeginDay()
    {
        EconomyManager.ResolveDelivery(store, config);
        results = new DayResults();
        clock.ResetForNewDay();
        notificationUI.HideAll();
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

    void HandleHourTick(int hour)
    {
        if (!gameStarted) return; // ignore ticks before a store actually exists

        // Update: fire the weather notification exactly once, the hour it actually starts affecting traffic.
        if (results.triggeredEvent != null && !weatherNotified && hour >= results.eventTriggerHour)
        {
            notificationUI.ShowWeatherBanner(results.triggeredEvent);
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

        EconomyManager.ResolveHourOfSales(store, results, customersThisHour, config, currentDay);

        int visualCount = Mathf.Min(customersThisHour, config.maxVisibleCustomersPerHour);
        customerSpawner.SpawnForHour(visualCount);

        if (hour >= config.closeHour) EndDay();
    }

    // Update: shows the "THEFT!" notification and actually instantiates the Thief prefab at the
    // store's spawn/exit points - previously theft only existed as a silent bool with no in-scene presence.
    void TriggerTheft()
    {
        notificationUI.ShowTheftBanner();

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
        currentDay++;
        store.baseCustomersToday += Random.Range(5, 11); // Random.Range(int,int) max is EXCLUSIVE, so this gives 5-10 inclusive
        BeginDay();
    }

    // Called by Thief.cs when the theft minigame resolves (see section 6)
    public void NotifyTheftResult(bool caught)
    {
        results.theftCaught = caught;
        results.theftResolved = true;
        notificationUI.HideTheftBanner();

        if (!caught)
        {
            foreach (var item in allItems)
            {
                int loss = Mathf.Min(store.currentStock.GetValueOrDefault(item), theftEvent.itemsLostIfEscaped);
                store.currentStock[item] -= loss;
            }
        }
    }
}