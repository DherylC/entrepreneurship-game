using UnityEngine;

public class Movement : MonoBehaviour
{
    public int directionX = 0;
    public int directionY = 0;

    public float speed = 2.0f;
    public float posX, posY;

    void Update()
    {
        transform.Translate(new Vector3(directionX, directionY, 0) * speed * Time.deltaTime);
        posX = transform.position.x;
        posY = transform.position.y;

        if (posX <= 0.0f && posY <= 0.0f)
        {
            directionX = 1;
            directionY = 0;
        } 
        else if (posX >= 5.0f && posY <= 0.0f)
        {
            directionX = 0;
            directionY = 1;
        }
        else if (posX >= 5.0f && posY >= 5.0f)
        {
            directionX = -1;
            directionY = 0;
        }
        else if (posX <= 0.0f && posY >= 5.0f)
        {
            directionX = 0;
            directionY = -1;
        } 
    }
}