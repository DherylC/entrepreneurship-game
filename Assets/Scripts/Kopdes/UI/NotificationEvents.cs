using System;

public static class NotificationEvents
{
    public static event Action<string> OnNotify;
    public static void Raise(string message) => OnNotify?.Invoke(message);
}