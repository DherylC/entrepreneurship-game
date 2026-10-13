using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform entryPoint;
    public Transform exitPoint;
    public Transform[] shelfWaypoints; // Update: now only a fallback, used when no routes are assigned (or a route is empty)
    public CustomerRoute[] routes;     // Update: each customer follows one of these at random
    public int poolSize = 10;

    Queue<GameObject> pool = new();
    LotSite myLot;

    void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(customerPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public void Initialize(LotSite lot) => myLot = lot;

    public void SpawnForHour(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;
            var obj = pool.Dequeue();
            obj.SetActive(true);
            obj.transform.position = entryPoint.position;

            // Update: pick a predetermined route if any are assigned. The single shelf is only passed along as
            // a fallback in case the chosen route turns out to be empty.
            CustomerRoute route = (routes != null && routes.Length > 0) ? routes[Random.Range(0, routes.Length)] : null;
            Transform fallbackShelf = (shelfWaypoints != null && shelfWaypoints.Length > 0) ? RandomShelf() : null;
            StartCoroutine(obj.GetComponent<CustomerVisual>().DoVisit(this, entryPoint, route, fallbackShelf, exitPoint));
        }
    }

    Transform RandomShelf() => shelfWaypoints[Random.Range(0, shelfWaypoints.Length)];

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}