using UnityEngine;

public class FPSController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 200f;
    private float xRotation = 0f;

    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform spawnPoint;
    public float bulletForce = 1500f;

    void Start()
    {
        // Kunci kursor ke tengah layar
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. Rotasi Kamera (Look Up/Down, Left/Right)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Batasi pandangan atas/bawah

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.parent?.Rotate(Vector3.up * mouseX);

        // 2. Menembak (Spasi atau Klik Kiri)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null) return;

        // Gunakan spawnPoint jika ada, atau gunakan posisi kamera
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward;

        // Buat instance peluru
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, transform.rotation);

        // Berikan gaya dorong ke depan
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(transform.forward * bulletForce);
        }

        // Hapus peluru setelah 3 detik agar hemat memori
        Destroy(bullet, 10f);
    }
}