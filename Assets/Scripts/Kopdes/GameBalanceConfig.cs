using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Config/GameBalanceConfig")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("Starting Values")]
    public float startingCash = 0f;

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