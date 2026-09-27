using UnityEngine;

public class GameplaySceneBootstrapper : MonoBehaviour
{
    public GameObject storePrefab;
    public Terrain terrain; // MUST use the same TerrainData asset as MapPlacement's terrain
    public DayManager dayManager;
    public GameplayCameraController gameplayCamera; // Update: snaps to the store once it's placed

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

        // Update: also find the store's theft spawn/exit points, same pattern as CustomerSpawner -
        // DayManager needs these to actually spawn the Thief when a theft event triggers.
        Transform theftSpawn = storeInstance.transform.Find("theftSpawnPoint");
        Transform theftExit = storeInstance.transform.Find("theftExitPoint");
        if (theftSpawn == null || theftExit == null)
            Debug.LogWarning("StorePrefab is missing a ThiefSpawnPoint/ThiefExitPoint child - theft events won't be able to spawn a Thief.");

        dayManager.StartGame(GameSessionData.ChosenLot, GameSessionData.ChosenLotData, spawner, theftSpawn, theftExit);

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