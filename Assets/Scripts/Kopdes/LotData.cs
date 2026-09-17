using UnityEngine;

[CreateAssetMenu(fileName = "LotData", menuName = "Config/LotData")]
public class LotData : ScriptableObject
{
    public string lotName;
    public float basePurchaseCost = 0f;

    [Header("Interpolated by proximity to city (0=far, 1=next to city)")]
    public float minTrafficMultiplier = 0f;
    public float maxTrafficMultiplier = 0f;
    public float minPriceMultiplier = 0f;
    public float maxPriceMultiplier = 0f;
    public float minEventChanceMultiplier = 0f;
    public float maxEventChanceMultiplier = 0f;
}