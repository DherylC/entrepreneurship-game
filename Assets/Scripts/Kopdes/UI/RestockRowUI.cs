using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestockRowUI : MonoBehaviour
{
    public TMP_Text itemNameText;
    public TMP_InputField quantityInput; // editable box, not plain display text
    public TMP_Text costText;
    public Button plusButton;
    public Button minusButton;

    ItemData item;
    int quantity;
    GameBalanceConfig config;
    StoreState store; // Update: needed to compute remaining capacity headroom for the clamp below
    System.Action onChanged; // lets ShoppingUI recalc the total whenever any row changes
    bool isLocked;

    // Update: now takes the StoreState too, so quantity can be clamped against the capacity cap.
    public void Initialize(ItemData itemData, GameBalanceConfig cfg, StoreState storeState, System.Action onChangedCallback, int currentDay)
    {
        item = itemData;
        config = cfg;
        store = storeState;
        onChanged = onChangedCallback;
        quantity = 0;
        isLocked = item.unlockOnDay > currentDay;

        itemNameText.text = item.itemName;

        // Locked items show but can't be interacted with - buttons/input disabled, cost text repurposed as a status message
        plusButton.interactable = !isLocked;
        minusButton.interactable = !isLocked;
        quantityInput.interactable = !isLocked;

        if (isLocked)
        {
            quantityInput.SetTextWithoutNotify("-");
            costText.text = $"Locked - Day {item.unlockOnDay}";
            return; // don't wire listeners on a locked row - nothing for the player to change yet
        }

        quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber; // blocks letters/symbols at the keyboard level
        plusButton.onClick.AddListener(() => ChangeQuantity(10));
        minusButton.onClick.AddListener(() => ChangeQuantity(-10));
        quantityInput.onEndEdit.AddListener(OnTypedQuantity); // fires when the player finishes typing (Enter or clicks away)

        Refresh();
    }

    // Update: how much MORE of this item can actually be ordered - the cap is on TOTAL stock,
    // not on the order alone, so this is (capacity - whatever's already sitting on the shelf).
    int MaxOrderableQuantity()
    {
        int cap = store.upgrades.MaxStockPerItem(config);
        int current = store.currentStock.GetValueOrDefault(item);
        return Mathf.Max(0, cap - current);
    }

    // Update: quantity now clamps to MaxOrderableQuantity() instead of just a 0 floor - any
    // attempt to go over the cap (via + button or typing) snaps straight down to the max allowed.
    void ChangeQuantity(int delta)
    {
        if (isLocked) return; // extra safety - buttons are already disabled, but guards direct calls too
        quantity = Mathf.Clamp(quantity + delta, 0, MaxOrderableQuantity());
        Refresh();
        onChanged?.Invoke();
    }

    void OnTypedQuantity(string typedValue)
    {
        if (isLocked) return;
        if (!int.TryParse(typedValue, out int parsed))
            parsed = 0; // empty or invalid input falls back to 0 instead of crashing

        quantity = Mathf.Clamp(parsed, 0, MaxOrderableQuantity());
        Refresh();
        onChanged?.Invoke();
    }

    void Refresh()
    {
        quantityInput.SetTextWithoutNotify(quantity.ToString()); // updates the box without re-triggering onEndEdit
        costText.text = (item.flatRestockCost * quantity).ToString("F0");
    }

    public int GetQuantity() => isLocked ? 0 : quantity; // locked rows never contribute to an order, even defensively
    public ItemData GetItem() => item;
}