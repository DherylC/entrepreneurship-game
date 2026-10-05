using UnityEngine;

public class w7_laser : MonoBehaviour
{
    private LineRenderer lr;
    private Animator heroAnimator;

    void Start()
    {
        lr = GetComponent<LineRenderer>();

        // Finds the w7_gerak script on the character root and gets its Animator
        w7_gerak hero = GetComponentInParent<w7_gerak>();
        if (hero != null)
        {
            heroAnimator = hero.GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (lr == null || heroAnimator == null) return;

        bool isAiming = heroAnimator.GetBool("stat_aim");

        if (isAiming)
        {
            lr.enabled = true;
            lr.SetPosition(0, transform.position);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 5000f))
            {
                lr.SetPosition(1, hit.point);
            }
            else
            {
                lr.SetPosition(1, transform.position + (transform.forward * 5000f));
            }
        }
        else
        {
            lr.enabled = false;
        }
    }
}