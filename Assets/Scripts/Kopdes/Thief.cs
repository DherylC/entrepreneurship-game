using UnityEngine;

public class Thief : MonoBehaviour
{
    public float moveSpeed = 3f;
    public DayManager dayManager; // drag the GameManager object in
    Transform exitPoint;
    bool caught;
    bool finished; // Update: set once caught OR escaped, so a late bullet can't report a second result

    public void Initialize(Transform spawnPoint, Transform exit)
    {
        transform.position = spawnPoint.position;
        exitPoint = exit;
        caught = false;
        finished = false;
    }

    void Update()
    {
        if (finished) return;
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
}