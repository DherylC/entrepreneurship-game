using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class w6_anim : MonoBehaviour
{
    private Animator anim;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public Transform cameraTransform;

    [Header("Attack Settings")]
    public float attackRadius = 1.8f;
    public LayerMask enemyLayer;
    public float hitCooldown = 0.5f;

    private float comboAtk = 0f;
    private float lastAtk = 0f;
    private float lastHitTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();

        // Auto-assign Main Camera if not set in Inspector
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // Challenge #3: Camera-Relative Movement
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * v + camRight * h;
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

            transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
            anim.SetBool("jalan", true);
        }
        else
        {
            anim.SetBool("jalan", false);
        }

        // Jump
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetBool("lompat", true);
        }

        // Combo Attack
        if (Input.GetButtonDown("Fire1"))
        {
            lastAtk = Time.time;
            comboAtk++;
            comboAtk = Mathf.Clamp(comboAtk, 0, 3);
            anim.SetFloat("serang", comboAtk);

            // Fallback hit check if not using Animation Events
            DealHitToEnemies();
        }

        // Reset Attack Combo
        if (Time.time - lastAtk >= 1f)
        {
            comboAtk = 0f;
            anim.SetFloat("serang", comboAtk);
        }
    }

    // Deals hit to nearby skeletons
    // Called by Animation Event on attack clips (atk01, atk03, atk04)
    // Called by Animation Event on attack clips (atk01, atk03, atk04)
    public void DealHitToEnemies()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward, attackRadius, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            w6_skeleton skeletonScript = enemy.GetComponent<w6_skeleton>();
            if (skeletonScript != null)
            {
                skeletonScript.TakeHit();
            }
        }
    }

    // Called when Skeleton attacks Hero
    public void TakeHit()
    {
        if (Time.time - lastHitTime >= hitCooldown)
        {
            lastHitTime = Time.time;
            anim.SetTrigger("isHit");
        }
    }

    // Called by Jump Animation Event
    public void reset_lompat()
    {
        anim.SetBool("lompat", false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRadius);
    }


}