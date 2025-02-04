using UnityEngine;
using UnityEngine.Serialization;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform Target;
    [SerializeField] private Vector2 Offset;
    [SerializeField][Range(0,10)] private float Tolerance = 2f;
    [SerializeField][Range(0,0.05f)] private float SmoothSpeed = 0.002f;

    private void LateUpdate()
    {
        var dir = (Vector2)Target.position - (Vector2)transform.position + Offset;
        var dist = dir.magnitude;
        if (dist <= Tolerance) return;
        // dist > tolerance => lerp to dir *(1-tolerance)/dist
        var targetPos = (Vector2)transform.position + dir * (dist - Tolerance);
        var newPos = Vector2.Lerp(transform.position, targetPos, SmoothSpeed);
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
    }
}
