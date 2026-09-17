using System.Collections;
using UnityEngine;

public class CustomerVisual : MonoBehaviour
{
    public float moveSpeed = 2f;

    public IEnumerator DoVisit(CustomerSpawner spawner, Transform entry, Transform shelf, Transform exit)
    {
        yield return MoveTo(shelf.position);
        yield return new WaitForSeconds(1.5f);
        yield return MoveTo(exit.position);
        spawner.ReturnToPool(gameObject);
    }

    IEnumerator MoveTo(Vector3 dest)
    {
        while (Vector3.Distance(transform.position, dest) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}