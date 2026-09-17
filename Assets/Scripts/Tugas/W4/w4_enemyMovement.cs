using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class w4_enemyMovement : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] point;
    [SerializeField] private float kec = 3f;
    [SerializeField] private float waitTimeAtEnd = 2f;

    [Header("Target & Range Settings")]
    [SerializeField] private Transform hero;
    [SerializeField] private float chaseRange = 7f;   // Jarak jangkauan musuh mendeteksi hero
    [SerializeField] private float chaseSpeed = 4.5f;

    private Rigidbody rb;
    private int idxPoint = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool isChasing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (hero == null)
        {
            GameObject heroObj = GameObject.FindWithTag("Player");
            if (heroObj != null) hero = heroObj.transform;
        }
    }

    void Update()
    {
        if (hero == null) return;

        // Hitung jarak ke hero
        float distanceToHero = Vector3.Distance(transform.position, hero.position);

        // Jika hero berada di dalam radius, aktifkan status chase
        if (distanceToHero <= chaseRange)
        {
            isChasing = true;
        }
        else
        {
            isChasing = false;
        }
    }

    void FixedUpdate()
    {
        if (isChasing)
        {
            ChaseHero();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        if (point == null || point.Length == 0) return;

        // Logika jeda diam di titik terakhir
        if (isWaiting)
        {
            waitTimer += Time.fixedDeltaTime;
            if (waitTimer >= waitTimeAtEnd)
            {
                isWaiting = false;
                waitTimer = 0f;
                idxPoint = 0;
            }
            return;
        }

        Vector3 posTarget = new Vector3(point[idxPoint].position.x, rb.position.y, point[idxPoint].position.z);
        Vector3 direction = posTarget - rb.position;

        // Gerak menuju movepoint
        Vector3 newPos = Vector3.MoveTowards(rb.position, posTarget, kec * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
        }

        // Cek jika sudah sampai di movepoint
        if (Vector3.Distance(new Vector3(rb.position.x, point[idxPoint].position.y, rb.position.z), point[idxPoint].position) < 0.2f)
        {
            idxPoint++;
            if (idxPoint >= point.Length)
            {
                idxPoint = point.Length - 1;
                isWaiting = true;
            }
        }
    }

    void ChaseHero()
    {
        // Reset timer jeda patroli agar musuh langsung bergerak mengejar
        isWaiting = false;
        waitTimer = 0f;

        Vector3 posHeroTarget = new Vector3(hero.position.x, rb.position.y, hero.position.z);
        Vector3 direction = posHeroTarget - rb.position;

        // Gerak mengejar Hero
        Vector3 newPos = Vector3.MoveTowards(rb.position, posHeroTarget, chaseSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
        }
    }

    // Menampilkan lingkaran radius deteksi di Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}