using System;

public static class StockEvents
{
    public static event Action OnStockChanged;
    public static void Raise() => OnStockChanged?.Invoke();
}