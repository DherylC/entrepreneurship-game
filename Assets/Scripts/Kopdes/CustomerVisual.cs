using System.Collections;
using UnityEngine;

public class CustomerVisual : MonoBehaviour
{
    public float moveSpeed = 2f;

    [Header("Shelf pause (seconds) - long enough for the idle/inspect clips to be seen")]
    public float shelfPauseMin = 2f;
    public float shelfPauseMax = 4f;

    [Header("Walk animation weights - RELATIVE, they get normalised (30/45/5 becomes 37.5/56/6 percent)")]
    public float walkingWeight = 30f;      // walkVariant 0
    public float standardWalkWeight = 45f; // walkVariant 1
    public float sneakWalkWeight = 5f;     // walkVariant 2

    const int IdleVariantCount = 4; // idleVariant 0 BreathingIdle, 1 HappyIdle, 2 InspectUpwards, 3 InspectDownwards

    CustomerSpawner currentSpawner;
    int visitId;
    Animator animator; // found lazily on the model (or a child) - needs bool params isWalking + isIdle

    public IEnumerator DoVisit(CustomerSpawner spawner, Transform entry, CustomerRoute route, Transform fallbackShelf, Transform exit)
    {
        currentSpawner = spawner;
        int myVisit = ++visitId; // any later Shoot() or re-spawn bumps visitId and invalidates this run
        SetVariant("idleVariant", 0); // a fresh customer starts on BreathingIdle

        BeginWalking();

        if (route != null && route.Points.Count > 0)
        {
            // Update: follow the route in hierarchy order. Walk points are just passed through,
            // inspect points stop and play a random idle/inspect clip.
            foreach (RoutePoint point in route.Points)
            {
                yield return MoveTo(point.transform.position, myVisit);
                if (myVisit != visitId) yield break;

                if (point.isInspect)
                {
                    yield return Inspect(point, myVisit);
                    if (myVisit != visitId) yield break;
                }
            }
        }
        else if (fallbackShelf != null)
        {
            // No usable route: the old behaviour - walk to one shelf, pause, leave.
            yield return MoveTo(fallbackShelf.position, myVisit);
            if (myVisit != visitId) yield break;

            StopWalking();
            yield return new WaitForSeconds(Random.Range(shelfPauseMin, shelfPauseMax));
            if (myVisit != visitId) yield break;

            BeginWalking();
        }

        yield return MoveTo(exit.position, myVisit);
        if (myVisit != visitId) yield break;

        spawner.ReturnToPool(gameObject);
    }

    // Stop at an inspect point: face the point's blue (forward) arrow and play a random idle/inspect clip.
    IEnumerator Inspect(RoutePoint point, int myVisit)
    {
        StopWalking();

        Quaternion face = Quaternion.Euler(0f, point.transform.eulerAngles.y, 0f);
        float wait = Random.Range(point.pauseMin, point.pauseMax);
        for (float t = 0f; t < wait && myVisit == visitId; t += Time.deltaTime)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, face, 6f * Time.deltaTime);
            yield return null;
        }

        if (myVisit != visitId) yield break; // shot or re-spawned while inspecting - don't touch the animator
        BeginWalking();
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

    // Pure movement + facing. The animation state is NOT touched here: route points chain together, so the
    // walk cycle has to keep playing between them instead of dropping to idle at every point.
    IEnumerator MoveTo(Vector3 dest, int myVisit)
    {
        while (myVisit == visitId && Vector3.Distance(transform.position, dest) > 0.1f)
        {
            // Turn to face the direction of travel so the walk animation doesn't look like sliding.
            // Assumes the model's forward is +Z.
            Vector3 dir = dest - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // Start (or resume) walking. ONE walk style is picked per walking stretch, not per point:
    // swapping styles mid-stride would pop.
    void BeginWalking()
    {
        SetVariant("walkVariant", PickWalkVariant());
        SetWalking(true);
    }

    // Stop to browse. The idle/inspect clip is picked BEFORE switching state, so the Idle blend tree
    // already has the right weights when the transition starts (otherwise it would pop from BreathingIdle).
    void StopWalking()
    {
        SetVariant("idleVariant", Random.Range(0, IdleVariantCount)); // int max is EXCLUSIVE, so 0-3
        SetWalking(false);
    }

    // Weighted pick: 0 Walking, 1 StandardWalk, 2 SneakWalk
    int PickWalkVariant()
    {
        float total = walkingWeight + standardWalkWeight + sneakWalkWeight;
        if (total <= 0f) return 0;

        float roll = Random.value * total;
        if (roll < walkingWeight) return 0;
        if (roll < walkingWeight + standardWalkWeight) return 1;
        return 2;
    }

    // The variant params are FLOATS because they drive 1D blend trees (see the NPCAnimator steps).
    void SetVariant(string param, int value)
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator != null) animator.SetFloat(param, value);
    }

    // Update: keeps isWalking and isIdle strictly opposite. If both were ever true, the Idle->Walking and
    // Walking->Idle transitions would both be satisfied and the character would flicker between them.
    void SetWalking(bool walking)
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;
        animator.SetBool("isWalking", walking);
        animator.SetBool("isIdle", !walking);
    }
}