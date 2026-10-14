using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public TMP_Text messageText;
    public DayManager dayManager;

    [Header("Scene names (must be in File > Build Settings)")]
    public string restartSceneName = "MapPlacement";
    public string mainMenuSceneName = "MainMenu";

    bool leaving; // stops a double-click loading the scene twice

    void OnEnable()
    {
        if (messageText != null && dayManager != null)
            messageText.text = $"STORE CLOSED\nToo many complaints on Day {dayManager.currentDay}.";
    }

    public void OnRestartClicked() => ResetAndLoad(restartSceneName);
    public void OnMainMenuClicked() => ResetAndLoad(mainMenuSceneName);

    public void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Application.Quit() does nothing in the Editor
#else
        Application.Quit();
#endif
    }

    public void OnQuitClicked() => OnExitClicked(); // old name, kept so existing wiring still works

    void ResetAndLoad(string sceneName)
    {
        if (leaving) return;
        leaving = true;

        GameSessionData.ResetAll();            // the only data that outlives a scene load
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None; // cursor state also survives scene loads
        Cursor.visible = true;

        SceneManager.LoadScene(sceneName);     // every scene object (store, cash, day, stock, upgrades) is rebuilt fresh
    }
}