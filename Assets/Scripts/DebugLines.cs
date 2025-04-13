using System.Linq;
using UnityEngine;

public class DebugLines : MonoBehaviour
{
    public float raycastDistance = 20f;
    public float circleRadius = 1f;

    public bool bFoundGround = false;
    public bool bCanAddCollider = false;

    public bool MoveToFloor = false;
    public ModelType ModelType;
    public string ModelFilename;

    private bool writeLogs = false;

    void Update()
    {
        if (bCanAddCollider)
        {
            var mc = transform.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = transform.gameObject.GetComponent<MeshFilter>().mesh;
            bCanAddCollider = false;
        }

        if (bFoundGround)
        {
            return;
        }

        TryToMove(0);

        if (!bFoundGround)
        {
            TryToMove(Vector3.kEpsilon);
        }

        writeLogs = false;
    }

    private void TryToMove(float yOffset)
    {
        Vector3 epsilon = new Vector3(0, yOffset, 0);
        var hitsDown = Physics.RaycastAll(transform.position + epsilon, Vector3.down, raycastDistance)
            .OrderBy(x => x.distance)
            .ToList();

        var hitsUp = Physics.RaycastAll(transform.position - epsilon, Vector3.up, raycastDistance)
            .OrderBy(x => x.distance)
            .ToList();

        // Draw a debug ray and circle.
        Debug.DrawRay(transform.position + epsilon, Vector3.down * raycastDistance, hitsDown.Any() ? Color.red : Color.green);
        DebugExtension.DebugCircle(transform.position + epsilon, Vector3.up, hitsDown.Any() ? Color.red : Color.green, circleRadius);

        Debug.DrawRay(transform.position - epsilon, Vector3.up * raycastDistance, hitsUp.Any() ? Color.red : Color.blue);
        DebugExtension.DebugCircle(transform.position - epsilon, Vector3.up, hitsUp.Any() ? Color.red : Color.blue, circleRadius);

        //move object to hit point
        foreach(var hit in hitsDown)
        {
            if(TryMove(hit, "down", yOffset))
            {
                break;
            }
        }

        if (!bFoundGround)
        {
            foreach (var hit in hitsUp)
            {
                if (TryMove(hit, "up", yOffset))
                {
                    break;
                }
            }
        }

        if (!bFoundGround)
        {
            DebugLog(yOffset, "n/a", "No hit");
        }
    }

    private void DebugLog(float yOffset, string direction, string message)
    {
        if (!writeLogs)
        {
            return;
        }

        string messagePrefix = $"DebugLines - {gameObject.name} (Parent={gameObject.transform.parent.name}) - (MoveToFloor={MoveToFloor}) - (Model={ModelType.ToString()}) - (Filename={ModelFilename}) - (dir={direction}) - (yOffset={yOffset}) - ";
        Debug.Log(messagePrefix + message);
    }

    private bool TryMove(RaycastHit hit, string direction, float yOffset)
    {
        if (hit.collider.CompareTag("NoRayCast"))
        {
            DebugLog(yOffset, direction, $"Found hit on ({hit.collider.gameObject.name}) but has NoRayCast!");

            return false;
        }

        //calculate bounds of object so it doesnt fall through the floor
        Bounds bounds = GetComponent<Renderer>().bounds;
        float halfHeight = bounds.extents.y;

        //sometimes pivot point isnt in the middle of the object, so we need to compoensate for that
        float pivotOffset = transform.position.y - bounds.center.y;

        //move object to hit point
        transform.position = new Vector3(transform.position.x, hit.point.y + halfHeight + pivotOffset, transform.position.z);

        bFoundGround = true;

        bCanAddCollider = true;

        DebugLog(yOffset, direction, "SUCCESS!");
        return true;
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

