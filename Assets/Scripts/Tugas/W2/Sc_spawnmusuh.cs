using UnityEngine;

public class Sc_spawnmusuh : MonoBehaviour 
{
    [SerializeField] GameObject musuh;
    [SerializeField] GameObject posisiAwalMusuh;

    int delaySpawn = 100;

    void Update()
    {
        delaySpawn--;
        if(delaySpawn == 0)
        {
            delaySpawn = 100;
            Vector3 offset = new Vector3(0f, 1f, 0f);
            Instantiate(musuh, posisiAwalMusuh.transform.position + offset, Quaternion.identity);
        }
    }
}
