using UnityEngine;

public class w5_cameralab : MonoBehaviour
{
    GameObject hero;

    // --- PENGATURAN ROTASI (Lihat Atas/Bawah & Kiri/Kanan) ---
    private float currentX = 0f;
    private float currentY = 20f;
    public float rotasiSpeedX = 4f;
    public float rotasiSpeedY = 4f;
    public float batasBawahAngle = -20f; // Limit lihat atas (ke arah langit)
    public float batasAtasAngle = 70f;   // Limit lihat bawah (ke arah tanah)

    // --- PENGATURAN ZOOM IN/OUT ---
    public float currentJarak = 5f;      // Jarak default kamera ke hero
    public float minJarak = 1.5f;        // Jarak paling dekat (Zoom in maksimal)
    public float maxJarak = 10f;         // Jarak paling jauh (Zoom out maksimal)
    public float zoomSpeed = 2f;

    // --- PENGATURAN TARGET ---
    public float tinggiOffset = 1.5f;    // Agar kamera melihat ke dada/kepala, bukan kaki

    void Start()
    {
        hero = GameObject.Find("hero");

        // Ambil rotasi awal kamera dari posisi hero agar tidak melompat saat pertama kali play
        currentX = hero.transform.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (hero == null) return;

        // 1. INPUT ROTASI (Mouse X dan Mouse Y)
        currentX += Input.GetAxis("Mouse X") * rotasiSpeedX;
        currentY -= Input.GetAxis("Mouse Y") * rotasiSpeedY; // Minus (-) agar pergerakan mouse natural

        // Batasi kamera agar tidak bisa mutar 360 derajat ke atas/bawah
        currentY = Mathf.Clamp(currentY, batasBawahAngle, batasAtasAngle);

        // 2. INPUT ZOOM (Mouse ScrollWheel)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentJarak -= scroll * zoomSpeed; // Mengurangi jarak saat scroll ke atas (Zoom In)

        // Batasi batas jarak zoom
        currentJarak = Mathf.Clamp(currentJarak, minJarak, maxJarak);

        // 3. HITUNG POSISI IDEAL KAMERA (Tanpa mempedulikan tembok dulu)
        Vector3 titikTarget = hero.transform.position + (Vector3.up * tinggiOffset);
        Quaternion rotasi = Quaternion.Euler(currentY, currentX, 0);
        Vector3 arahKamera = rotasi * Vector3.back; // Arah mundur dari target

        // Posisi di mana kamera seharusnya berada jika tidak ada tembok
        Vector3 posisiIdeal = titikTarget + (arahKamera * currentJarak);

        // 4. RAYCAST ANTI-TEMBUS TEMBOK
        RaycastHit hit;
        Debug.DrawRay(titikTarget, arahKamera * currentJarak, Color.red); // Garis merah untuk debugging di Scene

        // Tembakkan raycast dari hero ke posisi ideal kamera
        if (Physics.Raycast(titikTarget, arahKamera, out hit, currentJarak))
        {
            if (hit.transform.name == "tembok")
            {
                // Jika kena tembok, pindahkan kamera ke titik tabrakan 
                // Ditambah offset dari normal (hit.normal * 0.2f) agar kamera tidak masuk sedikit ke tembok
                this.transform.position = hit.point + (hit.normal * 0.2f);
            }
            else
            {
                // Jika terkena benda lain yang bukan "tembok", abaikan dan pergi ke posisi ideal
                this.transform.position = posisiIdeal;
            }
        }
        else
        {
            // Jika tidak kena apa-apa, kamera bebas berada di posisi ideal
            this.transform.position = posisiIdeal;
        }

        // 5. PASTIKAN KAMERA SELALU MELIHAT TARGET
        this.transform.LookAt(titikTarget);
    }
}