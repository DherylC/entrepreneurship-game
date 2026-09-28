using UnityEngine;

public class w6_anim : MonoBehaviour
{
    Animator anim;
    public void reset_lompat() {
        anim.SetBool("lompat", false);
    }

    void Start() {
        anim = this.GetComponent<Animator>();

    }

    void Update()
    {
        float v = Input.GetAxis("Vertical");
        if (v != 0)
        {
            anim.SetBool("jalan", true);
        }
        else
        {
            anim.SetBool("jalan", true);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetBool("lompat", true);
        }
    }
}
