using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public TMP_Text messageText;
    public DayManager dayManager;

    void OnEnable()
    {
        if (messageText != null && dayManager != null)
            messageText.text = $"STORE CLOSED\nToo many complaints on Day {dayManager.currentDay}.";
    }

    public void OnRestartClicked()
    {
        // Clear the previous session's lot so the player picks a fresh spot, not the one that failed.
        GameSessionData.ChosenLot = null;
        GameSessionData.ChosenLotData = null;
        SceneManager.LoadScene("MapPlacement");
    }

    public void OnQuitClicked() => Application.Quit();
}