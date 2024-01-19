using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLines : MonoBehaviour
{
    public float raycastDistance = 20f;
    public float circleRadius = 1f;
    private RaycastHit hit;

    private bool bFoundGround = false;

    void LateUpdate()
    {
        if (bFoundGround)
            return;
            
        // Cast a ray straight down.
        bool hitSomething = Physics.Raycast(transform.position, -Vector3.up, out hit, raycastDistance);
        
        
        
        // Draw a debug ray and circle.
        Debug.DrawRay(transform.position, -Vector3.up * raycastDistance, hitSomething ? Color.red : Color.green);
        DebugExtension.DebugCircle(transform.position, Vector3.up, hitSomething ? Color.red : Color.green, circleRadius);

        //move object to hit point
        if (hitSomething)
        {
            if (hit.collider.CompareTag("NoRayCast"))
                return;

            
            //calculate bounds of object so it doesnt fall through the floor
            Bounds bounds = GetComponent<Renderer>().bounds;
            float halfHeight = bounds.extents.y;
            float halfWidth = bounds.extents.x;
            float halfDepth = bounds.extents.z;

            // Add a small offset to the y-coordinate of the hit point.
            float yOffset = halfHeight - 0.01f;

            //move object to hit point
            transform.position = new Vector3(hit.point.x, hit.point.y + yOffset, hit.point.z);

            bFoundGround = true;

            var mc = transform.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = transform.gameObject.GetComponent<MeshFilter>().mesh;

        }

    }
}

public static class DebugExtension
{
    public static void DebugCircle(Vector3 position, Vector3 up, Color color, float radius = 1.0f)
    {
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
}