using UnityEngine;
using TMPro;

public class MapPlacementUI : MonoBehaviour
{
    public GameObject previewLabelRoot;
    public TMP_Text categoryText;
    public TMP_Text priceText;
    public TMP_Text trafficText;

    public void UpdatePreview(LotData lotData, bool isValid)
    {
        previewLabelRoot.SetActive(true);

        if (!isValid)
        {
            categoryText.text = "Cannot build here";
            priceText.text = "Too steep or too close to a ledge";
            trafficText.text = "";
        }
        else
        {
            categoryText.text = lotData.lotName;
            priceText.text = $"Rp {lotData.basePurchaseCost:F0}";
            trafficText.text = $"Traffic: {lotData.maxTrafficMultiplier:F1}x"; // rough indicator for this category
        }

        // Follow the cursor with a small offset so the label doesn't sit directly under the pointer
        previewLabelRoot.transform.position = Input.mousePosition + new Vector3(20, 20, 0);
    }

    public void HidePreview() => previewLabelRoot.SetActive(false);
}