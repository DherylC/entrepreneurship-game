using System.Collections;
using UnityEngine;

public class CustomerVisual : MonoBehaviour
{
    public float moveSpeed = 2f;

    CustomerSpawner currentSpawner;
    int visitId;

    public IEnumerator DoVisit(CustomerSpawner spawner, Transform entry, Transform shelf, Transform exit)
    {
        currentSpawner = spawner;
        int myVisit = ++visitId; // any later Shoot() or re-spawn bumps visitId and invalidates this run

        yield return MoveTo(shelf.position, myVisit);
        if (myVisit != visitId) yield break;

        yield return new WaitForSeconds(1.5f);
        if (myVisit != visitId) yield break;

        yield return MoveTo(exit.position, myVisit);
        if (myVisit != visitId) yield break;

        spawner.ReturnToPool(gameObject);
    }

    // Called by FirstPersonController. Returns true only if this customer was genuinely
    // mid-visit and got removed (false if it was already pooled/inactive).
    public bool Shoot()
    {
        if (!gameObject.activeInHierarchy || currentSpawner == null) return false;
        visitId++; // stops the running DoVisit coroutine at its next check
        currentSpawner.ReturnToPool(gameObject);
        return true;
    }

    IEnumerator MoveTo(Vector3 dest, int myVisit)
    {
        while (myVisit == visitId && Vector3.Distance(transform.position, dest) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}