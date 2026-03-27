using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Object;

namespace Utility
{
    public static class MaterialSafeMeshCombine
    {
        public static MeshFilter MeshCombine(this GameObject gameObject, bool destroyObjects = false)
        {
            Vector3 originalPosition = gameObject.transform.position;
            Quaternion originalRotation = gameObject.transform.rotation;
            Vector3 originalScale = gameObject.transform.localScale;
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;

            List<Material> materials = new List<Material>();
            List<List<CombineInstance>> combineInstanceLists = new List<List<CombineInstance>>();
            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.transform == gameObject.transform)
                {
                    continue;
                }

                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

                if (!meshRenderer ||
                    !meshFilter.sharedMesh ||
                    meshRenderer.sharedMaterials.Length != meshFilter.sharedMesh.subMeshCount)
                {
                    continue;
                }

                for (int s = 0; s < meshFilter.sharedMesh.subMeshCount; s++)
                {
                    string sharedMaterialName = meshRenderer.sharedMaterials[s].name;
                    int materialArrayIndex = materials.FindIndex(m => m.name == sharedMaterialName);
                    if (materialArrayIndex == -1)
                    {
                        materials.Add(meshRenderer.sharedMaterials[s]);
                        materialArrayIndex = materials.Count - 1;
                    }
                    combineInstanceLists.Add(new List<CombineInstance>());

                    CombineInstance combineInstance = new CombineInstance();
                    combineInstance.transform = meshRenderer.transform.localToWorldMatrix;
                    combineInstance.subMeshIndex = s;
                    combineInstance.mesh = meshFilter.sharedMesh;
                    combineInstanceLists[materialArrayIndex].Add(combineInstance);
                }
            }

            // Get / Create mesh filter & renderer
            MeshFilter meshFilterCombine = gameObject.GetComponent<MeshFilter>();
            if (meshFilterCombine == null)
            {
                meshFilterCombine = gameObject.AddComponent<MeshFilter>();
            }
            MeshRenderer meshRendererCombine = gameObject.GetComponent<MeshRenderer>();
            if (meshRendererCombine == null)
            {
                meshRendererCombine = gameObject.AddComponent<MeshRenderer>();
            }

            // Combine by material index into per-material meshes
            // also, Create CombineInstance array for next step
            Mesh[] meshes = new Mesh[materials.Count];
            CombineInstance[] combineInstances = new CombineInstance[materials.Count];

            for (int m = 0; m < materials.Count; m++)
            {
                CombineInstance[] combineInstanceArray = combineInstanceLists[m].ToArray();
                meshes[m] = new Mesh();
                meshes[m].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                meshes[m].CombineMeshes(combineInstanceArray, true, true);

                combineInstances[m] = new CombineInstance();
                combineInstances[m].mesh = meshes[m];
                combineInstances[m].subMeshIndex = 0;
            }

            // Combine into one
            meshFilterCombine.sharedMesh = new Mesh();
            meshFilterCombine.sharedMesh.CombineMeshes(combineInstances, false, false);

            // Destroy other meshes
            foreach (Mesh oldMesh in meshes)
            {
                oldMesh.Clear();
                DestroyImmediate(oldMesh);
            }

            // Assign materials
            Material[] materialsArray = materials.ToArray();
            meshRendererCombine.materials = materialsArray;

            if (destroyObjects)
            {
                IEnumerable<Transform> toDestroy = meshFilters.Select(m => m.transform);
                List<Transform> toSave = new List<Transform>(8);
                Transform child;
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    if (meshFilters[i].gameObject == gameObject)
                    {
                        continue;
                    }

                    //Check if any children should be saved
                    for (int c = 0; c < meshFilters[i].transform.childCount; c++)
                    {
                        child = meshFilters[i].transform.GetChild(c);
                        if (!toDestroy.Contains(child))
                        {
                            toSave.Add(child);
                        }
                    }

                    //Move toSave children to root object
                    for (int s = 0; s < toSave.Count; s++)
                    {
                        toSave[s].parent = gameObject.transform;
                    }
                    toSave.Clear();

                    DestroyImmediate(meshFilters[i].gameObject);
                }
            }
            else
            {
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    if (meshFilters[i].gameObject == gameObject)
                    {
                        continue;
                    }
                    DestroyImmediate(meshFilters[i].GetComponent<MeshRenderer>());
                    DestroyImmediate(meshFilters[i]);
                }
            }
            gameObject.transform.position = originalPosition;
            gameObject.transform.rotation = originalRotation;
            gameObject.transform.localScale = originalScale;
            return meshFilterCombine;
        }
    }
}
