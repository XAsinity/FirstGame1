using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 10f, -8f);
    public float smoothTime = 0.12f;
    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        transform.LookAt(target);
    }

    // Optional helper so other code can set the target without needing direct field access
    public void SetTarget(Transform t) => target = t;
}