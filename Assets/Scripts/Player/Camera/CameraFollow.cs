using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Drag your Player into this slot in the Inspector
    public Vector3 offset = new Vector3(0f, 10f, -10f);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate the desired position based on the player's position + offset
        Vector3 desiredPosition = target.position + offset;

        // Smoothly transition to the new position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}