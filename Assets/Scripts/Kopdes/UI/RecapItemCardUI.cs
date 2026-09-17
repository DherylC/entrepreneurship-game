using UnityEngine;
using TMPro;

public class RecapItemCardUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text remainingText;

    public void Initialize(ItemData item, int quantitySold, int remainingStock)
    {
        titleText.text = $"{quantitySold}x {item.itemName}";
        remainingText.text = $"{remainingStock} left in Shop";
    }
}