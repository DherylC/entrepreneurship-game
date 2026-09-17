using UnityEngine;

public class Sc_move1 : MonoBehaviour
{
    private float speed = 8f;
    private float acceleration = 12f; // Controls movement momentum responsiveness
    private float jumpForce = 8f;
    private float extraGravity = 25f;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 currentVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        // Ground detection sphere at character base
        Vector3 spherePosition = transform.position + Vector3.down * 0.05f;
        isGrounded = Physics.CheckSphere(spherePosition, 0.3f, ~0, QueryTriggerInteraction.Ignore);

        // Ground-restricted jump setup
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            isGrounded = false; // Immediately flag false to avoid double trigger
        }
    }

    void FixedUpdate()
    {
        // Extra downward gravity when falling
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (Camera.main != null)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveInput = (camForward * v + camRight * h).normalized;
            Vector3 targetVelocity = moveInput * speed;

            // Momentum: Smoothly interpolate current horizontal velocity to target velocity
            Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            currentVelocity = Vector3.Lerp(currentHorizontalVel, targetVelocity, acceleration * Time.fixedDeltaTime);

            rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);

            // Rotate character to match direction of movement
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveInput);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.fixedDeltaTime);
            }
        }
    }
}