using UnityEngine;

public class sc_turret : MonoBehaviour
{
    GameObject target;

    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    private float fireTimer = 0f;


    void Update()
    {
        if (target != null)
        {
            Vector3 posTarget = target.transform.position - this.transform.position;
            this.transform.rotation = Quaternion.LookRotation(posTarget);

            fireTimer += Time.deltaTime;
            if (fireTimer >= 1f / fireRate)
            {
                Shoot();
                fireTimer = 0f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "musuh")
        {
            target = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "musuh" && other.gameObject == target)
        {
            target = null;
        }
    }

    void Shoot()
    {
        if (projectilePrefab != null && firePoint != null && target != null)
        {
            GameObject bulletObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // Kirim target ke script peluru
            sc_peluru bullet = bulletObj.GetComponent<sc_peluru>();
            if (bullet != null)
            {
                bullet.SetTarget(target);
            }
        }
    }
}
