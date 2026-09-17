using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    System.Action onChanged; // lets ShoppingUI recalc the total whenever any row changes
    bool isLocked;

    // currentDay is now required - this row shows even for locked items, just non-interactive
    public void Initialize(ItemData itemData, GameBalanceConfig cfg, System.Action onChangedCallback, int currentDay)
    {
        item = itemData;
        config = cfg;
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

    void ChangeQuantity(int delta)
    {
        if (isLocked) return; // extra safety - buttons are already disabled, but guards direct calls too
        quantity = Mathf.Max(0, quantity + delta);
        // TODO: also clamp against store.upgrades.MaxStockPerItem(config) if you want the
        // buttons to respect the capacity cap - currently uncapped on both button and typed input.
        Refresh();
        onChanged?.Invoke();
    }

    void OnTypedQuantity(string typedValue)
    {
        if (isLocked) return;
        if (!int.TryParse(typedValue, out int parsed))
            parsed = 0; // empty or invalid input falls back to 0 instead of crashing

        quantity = Mathf.Max(0, parsed);
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