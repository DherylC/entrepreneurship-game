using UnityEngine;

public class w7_gerak : MonoBehaviour
{
    Vector3 maju = Vector3.zero;
    float kecepatan = 3f;

    [Header("LookAt IK Settings")]
    private Transform targetLook = null;
    public float maxLookAngle = 60f;
    public float maxLookDistance = 5f;

    private float currentIkWeight = 0f;
    public float ikWeightSpeed = 3f; // Speed of smooth blending

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
        {
            Vector3 targetDirection = new Vector3(h, 0f, v);
            targetDirection = Camera.main.transform.TransformDirection(targetDirection);
            targetDirection.y = 0.0f;

            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            transform.rotation = targetRotation;

            transform.position += Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up) * v * Time.deltaTime * kecepatan;
            transform.position += Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up) * h * Time.deltaTime * kecepatan;

            anim.SetBool("stat_jalan", true);
        }
        else
        {
            anim.SetBool("stat_jalan", false);
        }

        if (Input.GetMouseButton(1))
        {
            anim.SetBool("stat_aim", true);
        }
        else
        {
            anim.SetBool("stat_aim", false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("item"))
        {
            targetLook = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("item"))
        {
            targetLook = null;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (anim == null) return;

        bool isWalking = anim.GetBool("stat_jalan");
        bool isAiming = anim.GetBool("stat_aim");
        bool shouldLook = false;

        // Only look at the item if NOT walking AND NOT aiming
        if (!isWalking && !isAiming && targetLook != null)
        {
            Vector3 directionToTarget = targetLook.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            float distance = directionToTarget.magnitude;

            if (angle <= maxLookAngle && distance <= maxLookDistance)
            {
                shouldLook = true;
            }
        }

        // Smoothly fade IK weight to 0 when aiming or walking
        float targetWeight = shouldLook ? 1f : 0f;
        currentIkWeight = Mathf.MoveTowards(currentIkWeight, targetWeight, Time.deltaTime * ikWeightSpeed);

        if (targetLook != null)
        {
            anim.SetLookAtWeight(currentIkWeight, 0.3f, 0.8f, 1.0f, 0.5f);
            anim.SetLookAtPosition(targetLook.position);
        }
        else
        {
            anim.SetLookAtWeight(currentIkWeight);
        }
    }
}