using UnityEngine;

public class CarMoveRight : MonoBehaviour
{
    public float speed = 10f;
    public float resetX = 50f;
    public float endX = -50f;

    public static float speedMultiplier = 1f;

    private Vector3 startPosition;
    private Transform carTransform;

    void Awake()
    {
        carTransform = transform;
    }

    void Start()
    {
        startPosition = carTransform.position;
    }

    void Update()
    {
        Vector3 pos = carTransform.position;

        pos += carTransform.forward * speed * speedMultiplier * Time.deltaTime;

        if (pos.x < endX)
        {
            pos.x = resetX;
        }

        carTransform.position = pos;
    }

    public void ResetCarPosition()
    {
        carTransform.position = startPosition;
    }
}
