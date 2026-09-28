using UnityEngine;

[RequireComponent(typeof(Animator))]
public class w6_skeleton : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;
    private int currentWaypointIndex = 0;

    [Header("Detection & Attack Settings")]
    public Transform player;
    public float detectionRadius = 8f;
    public float attackRadius = 1.5f;
    public float chaseSpeed = 3.5f;
    public float attackCooldown = 1.5f;

    [Header("Hit & Stun Settings")]
    public float hitCooldown = 0.8f;
    public float stunDuration = 0.6f;
    private bool isStunned = false;

    private Animator anim;
    private float lastAttackTime;
    private float lastHitTime;

    void Start()
    {
        anim = GetComponent<Animator>();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null || isStunned) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRadius)
        {
            AttackPlayer();
        }
        else if (distanceToPlayer <= detectionRadius)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        anim.SetBool("isSerang", false);

        if (waypoints == null || waypoints.Length == 0)
        {
            anim.SetBool("isJalan", false);
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        direction.y = 0f;

        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, patrolSpeed * Time.deltaTime);

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        anim.SetBool("isJalan", true);

        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.3f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    void ChasePlayer()
    {
        anim.SetBool("isSerang", false);
        anim.SetBool("isJalan", true);

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        transform.position = Vector3.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
        }
    }

    void AttackPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Trigger the attack animation when cooldown allows
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            anim.SetBool("isJalan", false);
            anim.SetBool("isSerang", true);
        }
        else
        {
            anim.SetBool("isSerang", false);
        }
    }

    // Called by Animation Event on Skeleton's attack animation clip (DS_onehand_attack_A)
    public void DealHitToPlayer()
    {
        if (player == null || isStunned) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Only deal hit if player is still within attack range when the swing connects
        if (distanceToPlayer <= attackRadius + 0.5f)
        {
            w6_anim playerScript = player.GetComponent<w6_anim>();
            if (playerScript != null)
            {
                playerScript.TakeHit();
            }
        }
    }

    // Called when Hero strikes Skeleton
    public void TakeHit()
    {
        if (Time.time - lastHitTime >= hitCooldown)
        {
            lastHitTime = Time.time;
            isStunned = true;

            anim.SetBool("isJalan", false);
            anim.SetBool("isSerang", false);
            anim.SetTrigger("isHit");

            CancelInvoke(nameof(ResetStun));
            Invoke(nameof(ResetStun), stunDuration);
        }
    }

    void ResetStun()
    {
        isStunned = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}