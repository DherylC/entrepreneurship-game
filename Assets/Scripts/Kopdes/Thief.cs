using UnityEngine;

public class Thief : MonoBehaviour
{
    public float moveSpeed = 3f;
    public DayManager dayManager; // set by DayManager when instantiated
    Transform[] path;             // Waypoints remaining to walk in sequence
    int pathIndex;
    bool caught;
    bool finished;                // Set once caught OR escaped, so late bullets can't trigger duplicate events
    Animator animator;            // Found lazily on the model or child

    public void Initialize(ThiefRoute route)
    {
        if (route == null || route.Points.Count == 0) return;

        transform.position = route.SpawnPoint.position;

        // Path starts from the second waypoint since we spawn at point 0
        if (route.Points.Count >= 2)
        {
            path = new Transform[route.Points.Count - 1];
            for (int i = 1; i < route.Points.Count; i++)
            {
                path[i - 1] = route.Points[i];
            }
        }
        else
        {
            path = new[] { route.SpawnPoint };
        }

        pathIndex = 0;
        caught = false;
        finished = false;
        SetWalking(true);
    }

    void Update()
    {
        if (finished || path == null || path.Length == 0) return;

        Transform target = path[pathIndex];

        // Face movement direction
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

        // Move towards current waypoint
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        // Check if reached current waypoint
        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            pathIndex++;
            if (pathIndex >= path.Length) // Reached the final exit point
            {
                Escaped();
            }
        }
    }

    public void Caught()
    {
        if (finished) return;
        finished = true;
        caught = true;
        dayManager.NotifyTheftResult(caught: true);
        gameObject.SetActive(false);
    }

    void Escaped()
    {
        if (finished) return;
        finished = true;
        dayManager.NotifyTheftResult(caught: false);
        gameObject.SetActive(false);
    }

    void SetWalking(bool walking)
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;
        animator.SetBool("isWalking", walking);
        animator.SetBool("isIdle", !walking);
    }
}