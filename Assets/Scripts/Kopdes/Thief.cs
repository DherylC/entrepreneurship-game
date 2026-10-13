using UnityEngine;

public class Thief : MonoBehaviour
{
    public float moveSpeed = 3f;
    public DayManager dayManager; // drag the GameManager object in
    Transform exitPoint;
    bool caught;
    bool finished; // Update: set once caught OR escaped, so a late bullet can't report a second result
    Animator animator; // found lazily on the model (or a child) - needs bool params isWalking + isIdle

    public void Initialize(Transform spawnPoint, Transform exit)
    {
        transform.position = spawnPoint.position;
        exitPoint = exit;
        caught = false;
        finished = false;
        SetWalking(true); // Update: the thief walks the whole way to the exit
    }

    void Update()
    {
        if (finished) return;

        // Update: face the direction of travel (assumes the model's forward is +Z)
        Vector3 dir = exitPoint.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

        transform.position = Vector3.MoveTowards(transform.position, exitPoint.position, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, exitPoint.position) < 0.2f)
            Escaped();
    }

    public void Caught()
    {
        if (finished) return; // Update: bullets take time to land - the thief may have already escaped
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

    // Update: keeps isWalking and isIdle strictly opposite (see CustomerVisual.SetWalking for why).
    void SetWalking(bool walking)
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;
        animator.SetBool("isWalking", walking);
        animator.SetBool("isIdle", !walking);
    }
}