using UnityEngine;
using TMPro;

public class CashHUD : MonoBehaviour
{
    public DayManager dayManager;
    public TMP_Text cashText;
    public GameObject popupPrefab;
    public Transform popupSpawnPoint; // just below cashText

    void OnEnable() => CashEvents.OnCashChanged += HandleCashChanged;
    void OnDisable() => CashEvents.OnCashChanged -= HandleCashChanged;

    void Update()
    {
        if (dayManager != null && cashText != null)
            cashText.text = $"Rp {dayManager.store.cash:N0}";
    }

    void HandleCashChanged(float delta)
    {
        if (popupPrefab == null || popupSpawnPoint == null) return;
        var popupObj = Instantiate(popupPrefab, popupSpawnPoint.position, Quaternion.identity, popupSpawnPoint.parent);
        popupObj.GetComponent<CashPopupItem>().Show(delta);
    }
}