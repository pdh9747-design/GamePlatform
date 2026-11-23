
using UnityEngine;

public class CarMoveUp : MonoBehaviour
{
    public float speed = 10f;
    public float resetZ = -80f;
    public float endZ = 80f;

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

        pos.z += speed * CarMoveLeft.speedMultiplier * Time.deltaTime;

        if (pos.z > endZ)
        {
            pos.z = resetZ;
        }

        carTransform.position = pos;
    }

    public void ResetCarPosition()
    {
        carTransform.position = startPosition;
    }
}
