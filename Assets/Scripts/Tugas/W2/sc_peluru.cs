using UnityEngine;

public class sc_peluru : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 3f;
    private GameObject target;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void SetTarget(GameObject newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        if (target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            transform.LookAt(target.transform.position);

            // Jika posisi peluru sudah sangat dekat dengan target
            if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
            {
                Destroy(target);
                Destroy(gameObject);
            }
        }
        else
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "musuh")
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }
}