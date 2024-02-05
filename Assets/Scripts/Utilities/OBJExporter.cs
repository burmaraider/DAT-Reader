using UnityEngine;
using System.IO;
using System.Text;
using SFB;

public class OBJExporter : MonoBehaviour
{
    private int StartIndex = 0;

    public void OnEnable()
    {
        UIActionManager.OnExport += ExportBSP;
        UIActionManager.OnExportSelectedObject += ExportSelectedObject;
        UIActionManager.OnExportAll += ExportAll;
    }

    public void OnDisable()
    {
        UIActionManager.OnExport -= ExportBSP;
        UIActionManager.OnExportSelectedObject -= ExportSelectedObject;
        UIActionManager.OnExportAll -= ExportAll;
    }

    private void ExportAll()
    {
        //find all meshes under "Level" and export
        var level = GameObject.Find("Level");
        if (level)
        {
            var mr = level.GetComponent<MeshRenderer>();

            DoRuntimeExport(true, mr);
        }
    }

    private void ExportSelectedObject()
    {
        var selectedObject = transform.GetComponent<ObjectPicker>().selectedObject;

        if (selectedObject)
        {
            var mr = selectedObject.GetComponent<MeshRenderer>();
            DoRuntimeExport(true, mr);
        }
    }

    private void ExportBSP()
    {
        var bsp = GameObject.Find("PhysicsBSP");
        if (bsp)
        {
            DoRuntimeExport(true, bsp.GetComponent<MeshRenderer>());
        }
    }

    public void DoRuntimeExport(bool bMakeSubMeshes, MeshRenderer mr)
    {
        if (!mr)
        {
            return;
        }

        ExtensionFilter[] efExtensionFiler = new[] {
                new ExtensionFilter("Wavefront OBJ", "obj" )
            };

        string szMeshName = mr.name;
        string szFilename = StandaloneFileBrowser.SaveFilePanel("Save " + szMeshName, Directory.GetCurrentDirectory(), szMeshName, efExtensionFiler);

        StartIndex = 0;

        StringBuilder meshString = new StringBuilder();

        Transform t = mr.transform;

        Vector3 originalPosition = t.position;
        t.position = Vector3.zero;

        if (!bMakeSubMeshes)
        {
            meshString.Append("g ").Append(t.name).Append("\n");
        }
        meshString.Append(processTransform(t, bMakeSubMeshes));

        WriteToFile(meshString.ToString(), szFilename);

        t.position = originalPosition;

        StartIndex = 0;
    }


    private string MeshToString(MeshFilter mfMeshFilter, Transform t)
    {
        Quaternion rotation = t.localRotation;

        int nVertices = 0;
        Mesh mMesh = mfMeshFilter.sharedMesh;
        if (!mMesh)
        {
            return "####Error####";
        }
        Material[] mats = mfMeshFilter.GetComponent<Renderer>().sharedMaterials;

        StringBuilder sb = new StringBuilder();

        foreach (Vector3 aVertices in mMesh.vertices)
        {
            Vector3 v = t.TransformPoint(aVertices);
            nVertices++;
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, -v.z));
        }
        sb.Append("\n");
        foreach (Vector3 aNormals in mMesh.normals)
        {
            Vector3 v = rotation * aNormals;
            sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 aUV in mMesh.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", aUV.x, -aUV.y));
        }
        for (int material = 0; material < mMesh.subMeshCount; material++)
        {
            sb.Append("\n");
            sb.Append("o ").Append(mats[material].name).Append("\n");
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");

            int[] triangles = mMesh.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1 + StartIndex, triangles[i + 2] + 1 + StartIndex, triangles[i + 1] + 1 + StartIndex));
            }
        }

        StartIndex += nVertices;
        return sb.ToString();
    }

    private string processTransform(Transform t, bool makeSubmeshes)
    {
        StringBuilder szMeshString = new StringBuilder();

        szMeshString.Append("#" + t.name
                        + "\n#-------"
                        + "\n");

        if (makeSubmeshes)
        {
            szMeshString.Append("g ").Append(t.name).Append("\n");
        }

        MeshFilter mf = t.GetComponent<MeshFilter>();
        if (mf)
        {
            szMeshString.Append(MeshToString(mf, t));
        }

        for (int i = 0; i < t.childCount; i++)
        {
            szMeshString.Append(processTransform(t.GetChild(i), makeSubmeshes));
        }

        return szMeshString.ToString();
    }

    private void WriteToFile(string szString, string szFilename)
    {
        using (StreamWriter sw = new StreamWriter(szFilename))
        {
            sw.Write(szString);
        }
    }
}