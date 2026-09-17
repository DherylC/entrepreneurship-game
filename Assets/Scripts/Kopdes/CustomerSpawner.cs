using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform entryPoint;
    public Transform exitPoint;
    public Transform[] shelfWaypoints;
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
            StartCoroutine(obj.GetComponent<CustomerVisual>().DoVisit(this, entryPoint, RandomShelf(), exitPoint));
        }
    }

    Transform RandomShelf() => shelfWaypoints[Random.Range(0, shelfWaypoints.Length)];

    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}