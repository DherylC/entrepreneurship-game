using UnityEngine;

public class GameplaySceneBootstrapper : MonoBehaviour
{
    public GameObject storePrefab;
    public Terrain terrain; // MUST use the same TerrainData asset as MapPlacement's terrain
    public DayManager dayManager;
    public GameplayCameraController gameplayCamera; // Update: snaps to the store once it's placed
    public CameraModeManager cameraModeManager; // Update: handed the store's Guard once it's instantiated

    void Awake()
    {
        // Must match MapPlacement's MapSetup.CenterTerrainAtOrigin() exactly - if the two scenes'
        // terrains aren't positioned identically, the same (x,z) coordinate maps to different ground.
        Vector3 size = terrain.terrainData.size;
        terrain.transform.position = new Vector3(-size.x / 2f, 0f, -size.z / 2f);
    }

    void Start()
    {
        if (GameSessionData.ChosenLot == null)
        {
            Debug.LogError("No lot was chosen before loading MainGameplay - did you skip the MapPlacement scene?");
            return;
        }

        Vector2 chosenPos = GameSessionData.ChosenLot.position; // recorded (x, z) at the moment of placement
        float groundHeight = terrain.SampleHeight(new Vector3(chosenPos.x, 0f, chosenPos.y));
        Vector3 storeSpawnPosition = new Vector3(chosenPos.x, groundHeight, chosenPos.y);

        var storeInstance = Instantiate(storePrefab, storeSpawnPosition, Quaternion.identity);
        var spawner = storeInstance.GetComponentInChildren<CustomerSpawner>();
        spawner.Initialize(GameSessionData.ChosenLot);

        // Update: find the store's ThiefRoute component directly from the store instance.
        ThiefRoute thiefRoute = storeInstance.GetComponentInChildren<ThiefRoute>();
        if (thiefRoute == null)
            Debug.LogWarning("StorePrefab is missing a ThiefRoute component - theft events won't be able to spawn a Thief.");

        dayManager.StartGame(GameSessionData.ChosenLot, GameSessionData.ChosenLotData, spawner, thiefRoute);

        // Update: find the Guard humanoid inside the store and hand it to the camera mode toggle.
        var guard = storeInstance.GetComponentInChildren<FirstPersonController>(true);
        if (guard != null)
        {
            guard.dayManager = dayManager;
            if (cameraModeManager != null) cameraModeManager.guard = guard;
        }
        else
        {
            Debug.LogWarning("StorePrefab has no Guard (FirstPersonController) child - first person mode won't work.");
        }

        var cutaway = storeInstance.GetComponentInChildren<BuildingCutaway>(true);
        if (cutaway != null)
        {
            cutaway.gameplayCamera = gameplayCamera;
            cutaway.cameraModeManager = cameraModeManager;
        }

        // Update: snap the aerial gameplay camera to the store the moment it's instantiated,
        // so the player starts looking at their actual placed store, not wherever the camera
        // happened to be left in the scene by default.
        if (gameplayCamera != null)
        {
            gameplayCamera.storeTransform = storeInstance.transform;
            gameplayCamera.SnapToStore();
        }
    }
}