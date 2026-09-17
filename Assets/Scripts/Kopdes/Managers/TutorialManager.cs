public class TutorialManager : UnityEngine.MonoBehaviour
{
    public TutorialStepData[] steps;
    public UIManager uiManager;
    public bool tutorialSkipped;
    int currentStepIndex;

    public void TryAdvance(TutorialTrigger firedTrigger)
    {
        if (tutorialSkipped || currentStepIndex >= steps.Length) return;
        if (steps[currentStepIndex].trigger == firedTrigger)
        {
            uiManager.ShowTutorialMessage(steps[currentStepIndex].message);
            currentStepIndex++;
        }
    }

    public void SkipTutorial()
    {
        tutorialSkipped = true;
        uiManager.HideTutorialMessage();
    }
}