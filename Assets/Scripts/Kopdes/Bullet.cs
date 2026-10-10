using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 120f;
    public float maxLifetime = 3f; // safety net so a bullet can never linger forever

    [Tooltip("Rotation added on top of 'face the target'. Use this if the bullet mesh's tip doesn't point along its blue Z axis (try 90,0,0 or 0,90,0).")]
    public Vector3 rotationOffsetEuler = Vector3.zero;

    Vector3 target;
    Action onArrive;
    bool launched;

    public void Launch(Vector3 targetPoint, Action arriveCallback)
    {
        target = targetPoint;
        onArrive = arriveCallback;

        Vector3 dir = (target - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(rotationOffsetEuler);

        launched = true;
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (!launched) return;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude < 0.01f)
        {
            launched = false; // so the callback can never fire twice
            onArrive?.Invoke();
            Destroy(gameObject);
        }
    }
}