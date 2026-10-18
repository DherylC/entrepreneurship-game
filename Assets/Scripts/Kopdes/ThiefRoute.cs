using System.Collections.Generic;
using UnityEngine;

public class ThiefRoute : MonoBehaviour
{
    readonly List<Transform> points = new();

    public IReadOnlyList<Transform> Points => points;
    public Transform SpawnPoint => points.Count > 0 ? points[0] : null;
    public Transform ExitPoint => points.Count > 0 ? points[points.Count - 1] : null;

    void Awake()
    {
        foreach (Transform child in transform) points.Add(child);

        if (points.Count < 2)
            Debug.LogWarning($"ThiefRoute '{name}' needs at least 2 child points (first = spawn, last = exit).", this);
    }

    void OnDrawGizmos()
    {
        Transform prev = null;
        int count = transform.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);

            Gizmos.color = i == 0 ? Color.green : (i == count - 1 ? Color.red : new Color(1f, 0.6f, 0f));
            Gizmos.DrawSphere(child.position, i == 0 || i == count - 1 ? 0.3f : 0.18f);

            if (prev != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(prev.position, child.position);
            }
            prev = child;
        }
    }
}