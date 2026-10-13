using System.Collections.Generic;
using UnityEngine;

public class CustomerRoute : MonoBehaviour
{
    List<RoutePoint> points = new();

    // The points in hierarchy order. Built once when the store is instantiated.
    public IReadOnlyList<RoutePoint> Points => points;

    void Awake()
    {
        foreach (Transform child in transform)
        {
            RoutePoint p = child.GetComponent<RoutePoint>();
            if (p != null) points.Add(p);
        }

        int inspect = 0;
        foreach (RoutePoint p in points) if (p.isInspect) inspect++;
        int walk = points.Count - inspect;

        if (walk < 5 || walk > 10 || inspect < 2 || inspect > 3)
            Debug.LogWarning($"Route '{name}' has {walk} walk and {inspect} inspect points (aim for 5-10 walk, 2-3 inspect).", this);
    }

    // Draws the route in the Scene view so you can check it without pressing Play.
    void OnDrawGizmos()
    {
        Transform prev = null;
        foreach (Transform child in transform)
        {
            RoutePoint p = child.GetComponent<RoutePoint>();
            if (p == null) continue;

            Gizmos.color = p.isInspect ? Color.yellow : Color.green;
            Gizmos.DrawSphere(child.position, p.isInspect ? 0.25f : 0.15f);
            if (p.isInspect) Gizmos.DrawRay(child.position, child.forward * 0.8f);

            if (prev != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(prev.position, child.position);
            }
            prev = child;
        }
    }
}