using UnityEngine;

public class ZoneOverlay : MonoBehaviour
{
    public Transform cityCenter;
    public float mainRoadRadius = 30f;  // must match PlacementController's value
    public float midRangeRadius = 70f;  // must match PlacementController's value
    public float outerRadius = 120f;    // visual extent of the green "Remote" zone - doesn't need to be exact

    public Material redZoneMaterial;
    public Material yellowZoneMaterial;
    public Material greenZoneMaterial;

    GameObject redDisc, yellowDisc, greenDisc;

    void Start()
    {
        // Largest disc (green) sits lowest and gets drawn first; each smaller disc sits
        // slightly higher so they stack visually without any transparency z-fighting.
        greenDisc = CreateDisc("GreenZone", outerRadius, greenZoneMaterial, 0.02f);
        yellowDisc = CreateDisc("YellowZone", midRangeRadius, yellowZoneMaterial, 0.04f);
        redDisc = CreateDisc("RedZone", mainRoadRadius, redZoneMaterial, 0.06f);
    }

    GameObject CreateDisc(string name, float radius, Material mat, float yOffset)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = name;

        // Purely visual - removing the Collider means Physics.Raycast (used by PlacementController
        // to find the terrain under the cursor) will never accidentally hit a zone disc instead of the ground.
        Destroy(disc.GetComponent<Collider>());

        disc.transform.SetParent(transform);
        disc.transform.position = cityCenter.position + Vector3.up * yOffset;

        // Unity's built-in Cylinder primitive has radius 0.5 and height 2 by default,
        // so scale.x/z = radius*2 gives the correct diameter, and a small scale.y flattens it into a disc.
        disc.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
        disc.GetComponent<Renderer>().material = mat;
        return disc;
    }

    public void SetVisible(bool visible)
    {
        redDisc.SetActive(visible);
        yellowDisc.SetActive(visible);
        greenDisc.SetActive(visible);
    }
}