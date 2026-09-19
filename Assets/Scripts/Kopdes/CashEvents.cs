using System;

public static class CashEvents
{
    // delta: positive = gain (shown green with a +), negative = cost (shown red with a -)
    public static event Action<float> OnCashChanged;
    public static void Raise(float delta) => OnCashChanged?.Invoke(delta);
}