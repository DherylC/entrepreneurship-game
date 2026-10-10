using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Config/GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("Starting Values")]
    public float startingCash = 0f;
    public int day1StartingStockPerItem = 50; // Update: Day 1 starts with real stock instead of empty shelves, so sales can actually happen before the first restock
    public int lowStockThreshold = 5; // Update: triggers an "almost out of stock" notification when an item's remaining count crosses this value

    [Header("Day/Time")]
    public int openHour = 8;
    public int closeHour = 20;
    public float realSecondsPerDay = 210f;

    [Header("Delivery")]
    public float flatDeliveryFee = 0f;

    [Header("Customer Base")]
    public float baseCustomersPerDay = 0f;
    public int maxVisibleCustomersPerHour = 8;

    [Header("Sale Price Variance")]
    public float minSellMultiplier = 0.5f;
    public float maxSellMultiplier = 3.0f;

    [Header("Upgrades (max level 20 for all tracks)")]
    public int maxUpgradeLevel = 20;
    public UpgradeCurve capacityCurve;   // storage size
    public UpgradeCurve decorCurve;      // payment bonus
    public UpgradeCurve facilityCurve;   // traffic bonus

    [Header("Theft")]
    public float theftBaseDailyChance = 0f;

    [Header("Losing Condition")]
    public int maxComplaintsPerDay = 10; // reaching this many complaints in one day ends the game
    public int complaintsPerCustomerShot = 10; // Update: complaints added each time the player shoots a customer
}

[System.Serializable]
public class UpgradeCurve
{
    public float baseCost = 0f;
    public float growthRate = 1.15f;
    public float baseEffect = 0f;
    public float effectPerLevel = 0f;

    public float CostForLevel(int currentLevel) => baseCost * Mathf.Pow(growthRate, currentLevel);
    public float EffectAtLevel(int level) => baseEffect + (effectPerLevel * level);
}