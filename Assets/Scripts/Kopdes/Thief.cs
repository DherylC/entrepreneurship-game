using UnityEngine;

public class Thief : MonoBehaviour
{
    public float moveSpeed = 3f;
    public DayManager dayManager; // drag the GameManager object in
    Transform exitPoint;
    bool caught;

    public void Initialize(Transform spawnPoint, Transform exit)
    {
        transform.position = spawnPoint.position;
        exitPoint = exit;
        caught = false;
    }

    void Update()
    {
        if (caught) return;
        transform.position = Vector3.MoveTowards(transform.position, exitPoint.position, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, exitPoint.position) < 0.2f)
            Escaped();
    }

    public void Caught()
    {
        caught = true;
        dayManager.NotifyTheftResult(caught: true);
        gameObject.SetActive(false);
    }

    void Escaped()
    {
        dayManager.NotifyTheftResult(caught: false);
        gameObject.SetActive(false);
    }
}