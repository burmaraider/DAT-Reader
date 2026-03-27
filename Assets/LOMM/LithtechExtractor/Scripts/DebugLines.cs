using UnityEngine;

[ExecuteInEditMode]
public class DebugLines : MonoBehaviour
{
    public float raycastDistance = 20f;
    public float hitCircleRadius = 0.5f;
    public float missCircleRadius = 1f;
    public RaycastHit hit;

    private static void DebugCircle(Vector3 position, Color color, float radius = 1.0f)
    {
        Vector3 up = Vector3.up;

        up = up.normalized * radius;
        Vector3 _forward = Vector3.Slerp(up, -up, 0.5f);
        Vector3 _right = Vector3.Cross(up, _forward).normalized * radius;

        Matrix4x4 matrix = new Matrix4x4();

        matrix[0] = _right.x;
        matrix[1] = _right.y;
        matrix[2] = _right.z;

        matrix[4] = up.x;
        matrix[5] = up.y;
        matrix[6] = up.z;

        matrix[8] = _forward.x;
        matrix[9] = _forward.y;
        matrix[10] = _forward.z;

        Vector3 _lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
        Vector3 _nextPoint = Vector3.zero;

        color = color == default(Color) ? Color.white : color;

        for (var i = 0; i <= 90; i++)
        {
            _nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
            _nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
            _nextPoint.y = 0;

            _nextPoint = position + matrix.MultiplyPoint3x4(_nextPoint);

            Debug.DrawLine(_lastPoint, _nextPoint, color);

            _lastPoint = _nextPoint;
        }
    }

    private void OnDrawGizmos()
    {
        bool hitSomething = Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance);

        if (hitSomething)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, hit.point);
            DebugCircle(hit.point, Gizmos.color, hitCircleRadius);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastDistance);
            DebugCircle(transform.position, Gizmos.color, missCircleRadius);
        }
    }
}
