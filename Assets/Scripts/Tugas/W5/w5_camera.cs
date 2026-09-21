using UnityEngine;

public class w5_camera : MonoBehaviour
{
    GameObject hero;
    // Set the minimum angle to a NEGATIVE number to allow looking up at the sky
    public float minCameraAngle = -20f;

    // Keep max angle at 70 so it doesn't go completely over the head
    public float maxCameraAngle = 70f;

    // Orbit around the chest/head so looking up feels natural
    public float heightOffset = 1.5f;

    void Start()
    {
        hero = GameObject.Find("hero");
        this.transform.rotation = hero.transform.rotation;

        Vector3 posCam = hero.transform.localPosition;
        posCam -= this.transform.forward * 5f;
        posCam += this.transform.up * 3f;
        this.transform.position = posCam;

        this.transform.LookAt(hero.transform);
    }

    void Update()
    {
        movecam_by_mouse();
    }

    void movecam_by_mouse()
    {
        // Target a point near the character's head/chest instead of their feet
        Vector3 targetPos = hero.transform.position + (Vector3.up * heightOffset);

        // 1. Horizontal rotation (Left/Right)
        this.transform.RotateAround(targetPos, Vector3.up, Input.GetAxis("Mouse X") * 4f);

        // 2. Vertical rotation (Up/Down)
        float mouseY = Input.GetAxis("Mouse Y") * 4f;
        float currentPitch = this.transform.eulerAngles.x;

        if (currentPitch > 180f)
        {
            currentPitch -= 360f;
        }

        float nextPitch = currentPitch + mouseY;

        // Use the adjusted limits
        // This now clamps between -30 and 70
        nextPitch = Mathf.Clamp(nextPitch, minCameraAngle, maxCameraAngle);
        float allowedRotation = nextPitch - currentPitch;

        // Rotate around the new higher target position
        this.transform.RotateAround(targetPos, this.transform.right, allowedRotation);

        // Re-focus on the higher target position so the camera looks at the head/chest
        this.transform.LookAt(targetPos);
    }

    void movecam_by_keyboard()
    {
        float speed = 0f;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = 50;
        }
        if (Input.GetKey(KeyCode.RightShift))
        {
            speed = -50;
        }
        this.transform.LookAt(hero.transform);
        this.transform.RotateAround(hero.transform.position, Vector3.up, speed * Time.deltaTime);


        if (Input.GetKey(KeyCode.Alpha1))
        {
            if (Vector3.Distance(this.transform.position, hero.transform.position) > 1f)
            {
                this.transform.Translate(Vector3.forward * Time.deltaTime, Space.Self);
            }
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            if (Vector3.Distance(this.transform.position, hero.transform.position) < 5f)
            {
                this.transform.Translate(Vector3.back * Time.deltaTime, Space.Self);
            }
        }
    }
}
