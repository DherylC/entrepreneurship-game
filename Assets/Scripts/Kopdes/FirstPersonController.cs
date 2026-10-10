using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Rig parts")]
    public Transform eyePoint;      // empty child at head height, camera is parented here in first person
    public GameObject bodyModel;    // the humanoid mesh - hidden in first person
    public GameObject worldGun;     // gun in the humanoid's hand - visible in aerial view only
    public GameObject viewGun;      // gun that is a child of EyePoint - visible in first person only

    [Header("Movement / look")]
    public float moveSpeed = 5f;
    public float lookSensitivity = 2f;
    public float gravity = 20f;

    [Header("Shooting")]
    public float shootRange = 50f;
    public float fireCooldown = 0.4f;
    public LayerMask shootMask = ~0;
    public ParticleSystem muzzleFlash; // optional
    public AudioSource shotAudio;      // optional

    [Header("Recoil (makes a shot visible even with no muzzle flash wired)")]
    public float recoilDistance = 0.06f;
    public float recoilRecoverSpeed = 12f;

    [Header("Bullet (visible projectile; the hit is applied when it arrives)")]
    public GameObject bulletPrefab; // clean prefab with Bullet.cs - NO collider, NO Rigidbody
    public Transform muzzle;        // empty child of ViewGun at the barrel tip

    [Header("Set at runtime by GameplaySceneBootstrapper")]
    public DayManager dayManager;

    CharacterController controller;
    Camera cam;
    float verticalLook;
    float verticalVelocity;
    float nextFireTime;
    bool active;
    Vector3 viewGunRestPos;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (viewGun != null) viewGunRestPos = viewGun.transform.localPosition;
        ApplyVisuals(false); // start in aerial-view state: body + hand gun visible
    }

    // Called by CameraModeManager once the camera transition has finished.
    public void Enter(Camera c)
    {
        cam = c;
        active = true;
        verticalLook = 0f;
        if (viewGun != null) viewGun.transform.localPosition = viewGunRestPos;
        ApplyVisuals(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Called by CameraModeManager the moment the player leaves first person.
    public void Exit()
    {
        active = false;
        ApplyVisuals(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ApplyVisuals(bool firstPerson)
    {
        if (bodyModel != null) bodyModel.SetActive(!firstPerson);
        if (worldGun != null) worldGun.SetActive(!firstPerson);
        if (viewGun != null) viewGun.SetActive(firstPerson);
    }

    void Update()
    {
        if (!active) return;

        // Movement
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 move = transform.TransformDirection(Vector3.ClampMagnitude(input, 1f)) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity -= gravity * Time.deltaTime;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        // Look: body turns left/right, only the eye tilts up/down
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        verticalLook = Mathf.Clamp(verticalLook - mouseY, -80f, 80f);
        eyePoint.localEulerAngles = new Vector3(verticalLook, 0f, 0f);

        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime) Shoot();

        // Ease the view gun back to rest after a recoil kick.
        if (viewGun != null)
            viewGun.transform.localPosition = Vector3.Lerp(viewGun.transform.localPosition, viewGunRestPos, recoilRecoverSpeed * Time.deltaTime);
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireCooldown;
        if (muzzleFlash != null) muzzleFlash.Play();
        if (shotAudio != null) shotAudio.Play();
        if (viewGun != null) viewGun.transform.localPosition = viewGunRestPos - Vector3.forward * recoilDistance; // kick back toward the camera

        // Aim point: exactly the screen centre, where the crosshair is.
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool didHit = Physics.Raycast(ray, out RaycastHit hit, shootRange, shootMask, QueryTriggerInteraction.Ignore);
        Vector3 targetPoint = didHit ? hit.point : ray.origin + ray.direction * shootRange;

        // Decide what was hit NOW (InParent so a collider on any child of the model still counts),
        // but only apply it when the bullet visually arrives.
        Thief thief = didHit ? hit.collider.GetComponentInParent<Thief>() : null;
        CustomerVisual customer = didHit ? hit.collider.GetComponentInParent<CustomerVisual>() : null;

        System.Action onArrive = () =>
        {
            // thief/customer may have been destroyed or pooled while the bullet was in flight
            if (thief != null) { thief.Caught(); return; }
            if (customer != null && customer.Shoot() && dayManager != null)
                dayManager.RegisterCustomerShot();
        };

        Vector3 start = muzzle != null ? muzzle.position : ray.origin;
        if (bulletPrefab != null && (targetPoint - start).sqrMagnitude > 0.25f)
        {
            GameObject go = Instantiate(bulletPrefab, start, Quaternion.identity);
            Bullet bullet = go.GetComponent<Bullet>();
            if (bullet != null) bullet.Launch(targetPoint, onArrive);
            else { Destroy(go); onArrive(); } // prefab is missing Bullet.cs - still register the hit
        }
        else
        {
            onArrive(); // no bullet prefab assigned, or the target is right at the muzzle
        }
    }
}