using UnityEngine;

public class MapSetup : MonoBehaviour
{
    public Terrain terrain;
    public Transform cityCenter;

    public enum Quadrant { NorthEast, NorthWest, SouthEast, SouthWest }
    public Quadrant cityQuadrant = Quadrant.NorthEast; // predetermined - set once in the Inspector, never randomized

    void Awake()
    {
        CenterTerrainAtOrigin();
        PositionCityCenter();
    }

    void CenterTerrainAtOrigin()
    {
        Vector3 size = terrain.terrainData.size;
        terrain.transform.position = new Vector3(-size.x / 2f, 0f, -size.z / 2f);
        // World (0,0) is now the terrain's exact center - it spans from (-sizeX/2, -sizeZ/2) to (+sizeX/2, +sizeZ/2).
    }

    void PositionCityCenter()
    {
        Vector3 size = terrain.terrainData.size;
        float quarterX = size.x / 4f;
        float quarterZ = size.z / 4f;

        float x = (cityQuadrant == Quadrant.NorthEast || cityQuadrant == Quadrant.SouthEast) ? quarterX : -quarterX;
        float z = (cityQuadrant == Quadrant.NorthEast || cityQuadrant == Quadrant.NorthWest) ? quarterZ : -quarterZ;

        // Terrain.SampleHeight returns an absolute world-space height at the given world x/z,
        // already accounting for the terrain's own transform position set above.
        float y = terrain.SampleHeight(new Vector3(x, 0f, z));

        cityCenter.position = new Vector3(x, y, z);
    }
}