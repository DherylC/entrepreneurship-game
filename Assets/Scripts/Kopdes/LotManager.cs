using UnityEngine;
using UnityEngine.SceneManagement;

public class LotManager : MonoBehaviour
{
    public Transform cityCenter; // for computing proximity
    public string mainGameplaySceneName = "MainGameplay";

    // Called by LotMarker (or MapPlacementUI's confirm button) once the player has picked a spot.
    public void ConfirmLotSelection(Vector3 worldPosition, LotData lotData)
    {
        var lot = new LotSite
        {
            position = new Vector2(worldPosition.x, worldPosition.z),
            normalizedProximity = ComputeProximity(worldPosition)
        };

        GameSessionData.ChosenLot = lot;
        GameSessionData.ChosenLotData = lotData;

        SceneManager.LoadScene(mainGameplaySceneName);
    }

    float ComputeProximity(Vector3 pos)
    {
        float maxDistance = 100f; // TODO: tune to your map's actual size
        float dist = Vector3.Distance(pos, cityCenter.position);
        return Mathf.Clamp01(1f - (dist / maxDistance));
    }
}