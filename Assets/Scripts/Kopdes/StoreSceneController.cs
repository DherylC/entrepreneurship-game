using UnityEngine;

public class StoreSceneController : MonoBehaviour
{
    public GameObject[] decorTierObjects; // index = decor level tier, TODO: assign in Inspector

    public void RefreshVisuals(StoreUpgrades upgrades)
    {
        for (int i = 0; i < decorTierObjects.Length; i++)
            decorTierObjects[i].SetActive(i <= upgrades.decorLevel);
    }
}