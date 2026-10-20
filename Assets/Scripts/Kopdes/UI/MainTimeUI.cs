using TMPro;
using UnityEngine;

public class MainTimeUI : MonoBehaviour
{
    public TMP_Text MainTimeField;
    public DayManager dayManager;
    public GameClock clock;

    private void Update()
    {
        refreshGameClock();
    }

    void refreshGameClock()
    {
        if (MainTimeField != null) {
            MainTimeField.text = LiveGameClock();

        }
    }

    string LiveGameClock()
    {
        string s = $"Day: {dayManager.currentDay} - {clock.currentHour:00}:{clock.currentMinute:00}";
        return s;
    }
}
