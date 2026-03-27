using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Transform))]
public class BoundingBoxDrawer : Editor
{
    private void DrawBoxLines(Vector3[] c)
    {
        // Bottom
        Handles.DrawLine(c[0], c[1]);
        Handles.DrawLine(c[1], c[2]);
        Handles.DrawLine(c[2], c[3]);
        Handles.DrawLine(c[3], c[0]);

        // Top
        Handles.DrawLine(c[4], c[5]);
        Handles.DrawLine(c[5], c[6]);
        Handles.DrawLine(c[6], c[7]);
        Handles.DrawLine(c[7], c[4]);

        // Sides
        Handles.DrawLine(c[0], c[4]);
        Handles.DrawLine(c[1], c[5]);
        Handles.DrawLine(c[2], c[6]);
        Handles.DrawLine(c[3], c[7]);
    }

    void OnSceneGUI()
    {
        Transform t = (Transform)target;
        MeshFilter meshFilter = t.GetComponent<MeshFilter>();

        if (meshFilter != null)
        {
            Mesh mesh = meshFilter.sharedMesh;
            Bounds localBounds = mesh.bounds;

            // Get the 8 corners of the local bounds
            Vector3[] localCorners = new Vector3[8];
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;

            localCorners[0] = new Vector3(min.x, min.y, min.z);
            localCorners[1] = new Vector3(max.x, min.y, min.z);
            localCorners[2] = new Vector3(max.x, max.y, min.z);
            localCorners[3] = new Vector3(min.x, max.y, min.z);
            localCorners[4] = new Vector3(min.x, min.y, max.z);
            localCorners[5] = new Vector3(max.x, min.y, max.z);
            localCorners[6] = new Vector3(max.x, max.y, max.z);
            localCorners[7] = new Vector3(min.x, max.y, max.z);

            // Transform corners to world space
            for (int i = 0; i < 8; i++)
                localCorners[i] = t.localToWorldMatrix.MultiplyPoint3x4(localCorners[i]);

            // Draw edges of bounding box
            Handles.color = Color.cyan;

            DrawBoxLines(localCorners);

            // Optionally show min/max in world space
            Handles.Label(localCorners[0], "Min (local): " + min.ToString("F2"));
            Handles.Label(localCorners[6], "Max (local): " + max.ToString("F2"));
        }
    }
}
