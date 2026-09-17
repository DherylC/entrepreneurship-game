using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Config/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public float baseSellPrice = 0f;   // reference price; actual sale = random 50%-300% x decor bonus
    public float flatRestockCost = 0f; // fixed cost per unit, no randomness
    public int unlockOnDay = 0;        // 0 = always unlocked
}