using UnityEngine;

public enum TutorialTrigger
{
    OnGameStart, OnFirstShopPlacement, OnFirstCustomers, OnFirstDayRecap,
    OnStockDepleted, OnUpgradeShopOpened, OnFirstRainEvent, OnFirstTheft, OnDay2Start
}

[CreateAssetMenu(fileName = "TutorialStepData", menuName = "Config/TutorialStepData")]
public class TutorialStepData : ScriptableObject
{
    [TextArea] public string message;
    public TutorialTrigger trigger;
}