using UnityEngine;

// Put on each child of a route object. The child ORDER in the Hierarchy is the order the customer walks them.
public class RoutePoint : MonoBehaviour
{
    [Tooltip("Off = just walk through. On = stop here, face this object's blue (forward) arrow, and play a random idle/inspect clip.")]
    public bool isInspect;

    [Tooltip("Inspect points only: how long the customer stays, picked randomly between these.")]
    public float pauseMin = 2f;
    public float pauseMax = 4f;
}