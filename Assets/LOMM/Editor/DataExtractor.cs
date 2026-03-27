using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Utility;
using UnityEngine.Rendering;

public class DataExtractor : EditorWindow
{
    public static readonly bool CenterXAlignABCModels = true;
    public static readonly bool BottomYAlignABCModels = true;

    public static readonly bool ShowLogErrors = false;
    public static readonly float UnityScaleFactor = 0.02f;
    // World Objects should be shifted down 1 unit.
    // Since we scale things by UnityScaleFactor, we can just use that directly to get the offset.
    public static readonly Vector3 WorldObjectOffset = new Vector3(0, -UnityScaleFactor, 0); 
    public static readonly float MoveToFloorRaycastDistance = 20f;

    //public static readonly string ProjectFolder = "C:\\lomm\\data\\";
    public static readonly string ProjectFolder = @"C:\temp\LOMMConverted\OriginalUnrezzed\";

    public static readonly string MissingMaterialPath =         "Assets/LOMM/DefaultMaterials/MissingMaterial.mat";
    public static readonly string InvisibleMaterialPath =       "Assets/LOMM/DefaultMaterials/InvisibleMaterial.mat";
    public static readonly string CustomInvisibleMaterialPath = "Assets/LOMM/DefaultMaterials/CustomInvisibleMaterial.mat";
    public static readonly string GeneratedAssetsFolder =       "Assets/LOMM/GeneratedAssets";

    public static readonly string AudioClipPath = $"{GeneratedAssetsFolder}/AudioClips";

    public static readonly string TexturePath = $"{GeneratedAssetsFolder}/Textures";
    public static readonly string MaterialPath = $"{GeneratedAssetsFolder}/Materials";

    public static readonly string ABCMeshPath = $"{GeneratedAssetsFolder}/Meshes/ABCModels";
    public static readonly string ABCPrefabPath = $"{GeneratedAssetsFolder}/Prefabs/ABCModels";

    public static readonly string BSPMeshPath = $"{GeneratedAssetsFolder}/Meshes/BSPModels";
    public static readonly string BSPPrefabPath = $"{GeneratedAssetsFolder}/Prefabs/BSPModels";

    public static readonly Dictionary<string, string> ABCMaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "models\\player\\king.abc", "models\\player\\goodking.abc" },
        { "models\\lizardmanwarrior.abc", "models\\save\\lizardwarrior.abc" },
    };

    public static Material MissingMaterial { get; set; }
    public static Material InvisibleMaterial { get; set; }
    public static Material CustomInvisibleMaterial { get; set; }

    [MenuItem("Tools/Generate All Assets (fast)")]
    public static void ExtractAllFast()
    {
        ExtractAll(false);
    }

    [MenuItem("Tools/Generate All Assets (slow - recreate)")]
    public static void ExtractAllSlow()
    {
        ExtractAll(true);
    }

    public static void ExtractAll(bool alwaysCreate)
    {
        System.Diagnostics.Stopwatch totalWatch = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        string stats = "Beginning of extract all. Using project path: " + ProjectFolder + "\r\n";
        if (!CreateDefaultMaterials())
        {
            return;
        }
        stats += watch.GetElapsedTime("CreateDefaultMaterials\r\n", 1);

        CreateGeneratedPaths();
        stats += watch.GetElapsedTime("CreateGeneratedPaths\r\n", 1);

        AudioLookupUtility.SetLookups(alwaysCreate); stats += watch.GetElapsedTime("AudioLookupUtility.SetLookups\r\n", 1);

        TextureLookupUtility.SetLookups(alwaysCreate); stats += watch.GetElapsedTime("TextureLookupUtility.SetLookups\r\n", 1);
        var datModels = GetAllDATModels(); stats += watch.GetElapsedTime("GetAllDATModels\r\n", 1);
        var sprModels = GetAllSPRModels(); stats += watch.GetElapsedTime("GetAllSPRModels\r\n", 1);
        MaterialLookupUtility.SetLookups(alwaysCreate, datModels, sprModels); stats += watch.GetElapsedTime("MaterialLookupUtility.SetLookups\r\n", 1);

        var abcModels = GetABCModels();
        ABCMeshLookupUtility.SetLookups(alwaysCreate, abcModels); stats += watch.GetElapsedTime("ABCMeshLookupUtility.SetLookups\r\n", 1);
        // ABCPrefabLookupUtility.SetLookups(alwaysCreate, datModels, abcModels); stats += watch.GetElapsedTime("ABCLookupUtility.SetLookups\r\n", 1);

        //CreateAssetsFromDATModels(datModels); stats += watch.GetElapsedTime("CreateAssetsFromDATModels\r\n", 1);

        stats += totalWatch.GetElapsedTime("Total Processing Time\r\n");
        Debug.Log(stats);
    }

    private static bool CreateDefaultMaterials()
    {
        MissingMaterial = AssetDatabase.LoadAssetAtPath<Material>(MissingMaterialPath);
        InvisibleMaterial = AssetDatabase.LoadAssetAtPath<Material>(InvisibleMaterialPath);
        CustomInvisibleMaterial = AssetDatabase.LoadAssetAtPath<Material>(CustomInvisibleMaterialPath);

        return MissingMaterial != null && InvisibleMaterial != null && CustomInvisibleMaterial != null;
    }

    private static void CreateGeneratedPaths()
    {
        Directory.CreateDirectory(TexturePath);
        Directory.CreateDirectory(MaterialPath);
        Directory.CreateDirectory(ABCMeshPath);
        Directory.CreateDirectory(ABCPrefabPath);
        Directory.CreateDirectory(BSPMeshPath);
        Directory.CreateDirectory(BSPPrefabPath);
        Directory.CreateDirectory(AudioClipPath);
    }

    protected static List<ABCModel> GetABCModels()
    {
        var abcFiles = Directory.GetFiles(ProjectFolder, "*.abc", SearchOption.AllDirectories);
        var abcModels = new List<ABCModel>();
        int i = 0;
        foreach (var abcFile in abcFiles)
        {
            i++;
            float progress = (float)i / abcFiles.Length;
            EditorUtility.DisplayProgressBar("Loading and processing ABC Models", $"Item {i} of {abcFiles.Length}", progress);

            var abcModel = ABCModelReader.ReadABCModel(abcFile, ProjectFolder);
            if (abcModel != null)
            {
                abcModels.Add(abcModel);
            }
        }

        EditorUtility.ClearProgressBar();

        return abcModels;
    }

    protected static List<SPRModel> GetAllSPRModels()
    {
        var files = Directory.GetFiles(ProjectFolder, "*.spr", SearchOption.AllDirectories);
        var models = new List<SPRModel>();
        int i = 0;
        foreach (var file in files)
        {
            i++;
            float progress = (float)i / files.Length;
            EditorUtility.DisplayProgressBar("Loading and processing SPR files", $"Item {i} of {files.Length}", progress);

            var relativePath = Path.GetRelativePath(ProjectFolder, file);
            SPRModel model = SPRModelReader.ReadSPRModel(ProjectFolder, relativePath);
            if (model != null)
            {
                models.Add(model);
            }
        }

        EditorUtility.ClearProgressBar();

        return models;
    }

    protected static List<UnityDTXModel> GetAllUnityDTXModels()
    {
        var files = Directory.GetFiles(ProjectFolder, "*.dtx", SearchOption.AllDirectories);
        var models = new List<UnityDTXModel>();
        int i = 0;
        foreach (var file in files)
        {
            i++;
            float progress = (float)i / files.Length;
            EditorUtility.DisplayProgressBar("Loading and processing DTX Textures", $"Item {i} of {files.Length}", progress);

            var dtxModel = DTXModelReader.ReadDTXModel(file, Path.GetRelativePath(ProjectFolder, file));
            if (dtxModel != null)
            {
                var model = DTXConverter.ConvertDTX(dtxModel);
                if (model != null)
                {
                    models.Add(model);
                }
            }
        }

        EditorUtility.ClearProgressBar();

        return models;
    }

    protected static List<DATModel> GetAllDATModels()
    {
        var files = Directory.GetFiles(ProjectFolder, "*.dat", SearchOption.AllDirectories);
        var models = new List<DATModel>();
        int i = 0;
        foreach (var file in files)
        {
            i++;
            float progress = (float)i / files.Length;
            EditorUtility.DisplayProgressBar("Loading and processing DAT files", $"Item {i} of {files.Length}", progress);

            var datModel = DATModelReader.ReadDATModel(file, ProjectFolder, Game.LOMM);
            if (datModel != null)
            {
                datModel.SetAllWorldProperties();
                models.Add(datModel);
            }
        }

        EditorUtility.ClearProgressBar();

        return models;
    }

    protected static void RefreshAssetDatabase(bool stopAssetEditing = true)
    {
        EditorUtility.DisplayProgressBar("Refreshing Assets", "Please wait while Unity updates the asset database...", 0.5f);
        System.Threading.Thread.Sleep(50);
        if (stopAssetEditing)
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();
        System.Threading.Thread.Sleep(50);
        EditorUtility.ClearProgressBar();
    }

    private static void AddColliders(GameObject rootObject)
    {
        // Assign the mesh collider to the combined meshes
        foreach (var meshFilter in rootObject.GetComponentsInChildren<MeshFilter>())
        {
            var meshCollider = meshFilter.transform.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }
    }

    private static bool IsTextureCustomInvisible(string textureName, bool isSky, bool showSurface, bool visible)
    {
        if (isSky || (!showSurface && !visible))
        {
            return true;
        }

        if (textureName.Contains("Invisible.dtx", StringComparison.OrdinalIgnoreCase)
               || textureName.Contains("Sky.dtx", StringComparison.OrdinalIgnoreCase)
               || textureName.Contains("Rain.dtx", StringComparison.OrdinalIgnoreCase)
               || textureName.Contains("hull.dtx", StringComparison.OrdinalIgnoreCase)
               || textureName.Contains("occluder.dtx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static WorldSurfaceModel GetSurface(BSPModel bspModel, WorldPolyModel poly, string datName, int polyIndex)
    {
        if (bspModel.TextureNames == null || bspModel.TextureNames.Count == 0)
        {
            Debug.Log($"Error creating BSP for DAT {datName}, BSP WorldName {bspModel.WorldName}. No textures found on poly {polyIndex}");
            return null;
        }

        if (poly.SurfaceIndex > bspModel.Surfaces.Count - 1)
        {
            Debug.Log($"Error creating BSP for DAT {datName}, BSP WorldName {bspModel.WorldName}. Invalid surface index on poly {polyIndex}");
            return null;
        }

        var surface = bspModel.Surfaces[poly.SurfaceIndex];
        if (surface == null)
        {
            Debug.Log($"Error creating BSP for DAT {datName}, BSP WorldName {bspModel.WorldName}. Could not find surface index on poly {polyIndex}");
            return null;
        }

        if (surface.TextureIndex > bspModel.TextureNames.Count - 1)
        {
            Debug.Log($"Error creating BSP for DAT {datName}, BSP WorldName {bspModel.WorldName}. Invalid texture index on poly {polyIndex}");
            return null;
        }

        if (string.IsNullOrEmpty(bspModel.TextureNames[surface.TextureIndex]))
        {
            Debug.Log($"Error creating BSP for DAT {datName}, BSP WorldName {bspModel.WorldName}. Texture name missing or invalid on poly {polyIndex}");
            return null;
        }

        return surface;
    }

    private static void CreateChildMeshes(WorldPolyModel poly, BSPModel bspModel, int index, Material material, WorldSurfaceModel surface, Transform parentTransform)
    {
        // Convert OPQ to UV magic
        Vector3 center = poly.Center;
        Vector3 o = surface.UV1;
        Vector3 p = surface.UV2;
        Vector3 q = surface.UV3;

        o *= UnityScaleFactor;
        o -= (Vector3)poly.Center;
        p /= UnityScaleFactor;
        q /= UnityScaleFactor;

        for (int nTriIndex = 0; nTriIndex < poly.LoVerts - 2; nTriIndex++)
        {
            Vector3[] vertexList = new Vector3[poly.LoVerts];
            Vector3[] vertexNormalList = new Vector3[poly.LoVerts];
            Color[] vertexColorList = new Color[poly.LoVerts];
            Vector2[] uvList = new Vector2[poly.LoVerts];
            int[] triangleIndices = new int[3];

            GameObject bspChildObject = new GameObject(bspModel.WorldName + index);
            bspChildObject.isStatic = true;
            bspChildObject.transform.parent = parentTransform;
            MeshRenderer meshRenderer = bspChildObject.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = bspChildObject.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();
            for (int vertIndex = 0; vertIndex < poly.LoVerts; vertIndex++)
            {
                var vertex = bspModel.Vertices[(int)poly.VertexColorList[vertIndex].VertexCount];

                Vector3 vertexData = vertex;
                vertexData *= UnityScaleFactor;
                vertexList[vertIndex] = vertexData;

                Color color = new Color(
                    poly.VertexColorList[vertIndex].R / 255,
                    poly.VertexColorList[vertIndex].G / 255,
                    poly.VertexColorList[vertIndex].B / 255,
                    1.0f);
                vertexColorList[vertIndex] = color;
                vertexNormalList[vertIndex] = bspModel.Planes[poly.PlaneIndex].Normal;

                // Calculate UV coordinates based on the OPQ vectors
                // Note that since the worlds are offset from 0,0,0 sometimes we need to subtract the center point
                Vector3 curVert = vertexList[vertIndex];
                float u = Vector3.Dot((curVert - center) - o, p);
                float v = Vector3.Dot((curVert - center) - o, q);

                //Scale back down into something more sane
                u /= material.mainTexture.width;
                v /= material.mainTexture.height;

                var uv = new Vector2(u, v);
                // Flip UV upside down - difference between Lithtech and Unity systems.
                uv.y = 1f - uv.y;
                uvList[vertIndex] = uv;
            }

            mesh.SetVertices(vertexList);
            mesh.SetNormals(vertexNormalList);
            mesh.SetUVs(0, uvList);
            mesh.SetColors(vertexColorList);

            // Hacky, whatever
            triangleIndices[0] = 0;
            triangleIndices[1] = nTriIndex + 1;
            triangleIndices[2] = (nTriIndex + 2) % poly.LoVerts;

            // Set triangles
            mesh.SetTriangles(triangleIndices, 0);
            mesh.RecalculateTangents();

            meshRenderer.sharedMaterial = material;
            meshFilter.sharedMesh = mesh;

            meshRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        }
    }

    private static GameObject CreateGameObject(string name, GameObject parent)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.parent = parent.transform;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        return gameObject;
    }

    private static bool CreateBSPChildMeshes(BSPModel bspModel, GameObject bspObject, WorldObjectModel worldObjectModel)
    {
        if (bspModel.Surfaces == null || bspModel.Surfaces.Count == 0)
        {
            Debug.Log($"Error creating BSP for DAT {bspObject.name}, BSP WorldName {bspModel.WorldName}. No surfaces found.");
            return false;
        }

        float? surfaceAlpha = (worldObjectModel == null || worldObjectModel.WorldObjectType != WorldObjectTypes.VisibleVolume)
            ? null
            : worldObjectModel.SurfaceAlpha;

        bool visible = worldObjectModel == null ? true : worldObjectModel.Visible;
        bool showSurface = worldObjectModel == null ? true : worldObjectModel.ShowSurface || visible;

        int polyIndex = -1;
        List<string> missingTextures = new List<string>();
        bool isPhysicsBSP = bspModel.WorldName == "PhysicsBSP";
        Dictionary<Material, GameObject> materialMaps = new Dictionary<Material, GameObject>();
        foreach (WorldPolyModel poly in bspModel.Polies)
        {
            polyIndex++;
            var surface = GetSurface(bspModel, poly, bspObject.name, polyIndex);
            if (surface == null)
            {
                continue;
            }

            var textureName = bspModel.TextureNames[surface.TextureIndex];
            Material material = GetMaterial(textureName, surface, surfaceAlpha, showSurface, visible);

            if (material == MissingMaterial)
            {
                missingTextures.Add(textureName);
            }

            GameObject parentObject;
            if (!isPhysicsBSP)
            {
                parentObject = bspObject;
            }
            else
            {
                if (materialMaps.TryGetValue(material, out var matGameObject))
                {
                    parentObject = matGameObject;
                }
                else
                {
                    var newGameObject = CreateGameObject(material.name, bspObject);
                    materialMaps.Add(material, newGameObject);
                    parentObject = newGameObject;
                }
            }

            CreateChildMeshes(poly, bspModel, polyIndex, material, surface, parentObject.transform);
        }

        if (ShowLogErrors)
        {
            if (missingTextures.Count > 0)
            {
                missingTextures = missingTextures.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var s = $"BSP ({bspModel.WorldName}) has missing textures:\r\n";
                foreach (var missingTexture in missingTextures)
                {
                    s += $"\t{missingTexture}\r\n";
                }

                Debug.Log(s);
            }
        }

        return isPhysicsBSP;
    }

    private static Material GetMaterial(string textureName, WorldSurfaceModel surface, float? surfaceAlpha, bool showSurface, bool visible)
    {
        bool isInvisibleFlag = (surface.Flags & (int)BitMask.INVISIBLE) == (int)BitMask.INVISIBLE;
        if (isInvisibleFlag)
        {
            return InvisibleMaterial;
        }

        if (string.IsNullOrEmpty(textureName))
        {
            return MissingMaterial;
        }

        bool isSky = (surface.Flags & (int)BitMask.SKY) == (int)BitMask.SKY;
        if (IsTextureCustomInvisible(textureName, isSky, showSurface, visible))
        {
            return CustomInvisibleMaterial;
        }

        if (surfaceAlpha.HasValue)
        {
            var surfaceAlphaMaterial = UnityLookups.GetMaterial(textureName, surfaceAlpha.Value);
            if (surfaceAlphaMaterial != null)
            {
                return surfaceAlphaMaterial;
            }
        }

        var material = UnityLookups.GetMaterial(textureName);
        if (material != null)
        {
            return material;
        }

        return MissingMaterial;
    }

    private static void CreateBSPMeshAndPrefab(GameObject datRootObject, List<GameObject> bspObjects)
    {
        // Save meshes
        string baseMeshPath = Path.Combine(BSPMeshPath, datRootObject.name);
        Directory.CreateDirectory(baseMeshPath);
        foreach(var bspObject in bspObjects)
        {
            string meshPathAndFilename = Path.Combine(baseMeshPath, bspObject.name + ".asset");
            var meshFilter = bspObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                var mesh = meshFilter.sharedMesh;
                AssetDatabase.CreateAsset(mesh, meshPathAndFilename);
            }
        }

        // Save prefab
        string prefabPathAndFilename = Path.Combine(BSPPrefabPath, datRootObject.name + ".prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(datRootObject, prefabPathAndFilename);
    }

    private static void CombineMeshesPreserveMaterials(string datName, GameObject parent)
    {
        var meshFilters = parent.GetComponentsInChildren<MeshFilter>(includeInactive: true);

        // Group by material
        Dictionary<Material, List<CombineInstance>> materialToCombine = new();

        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null || meshFilter.gameObject == parent)
            {
                continue;
            }

            var renderer = meshFilter.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterials.Length != 1)
            {
                Debug.LogWarning($"Skipping {meshFilter.name} — requires exactly one material.");
                continue;
            }

            Material mat = renderer.sharedMaterial;

            if (!materialToCombine.ContainsKey(mat))
            {
                materialToCombine[mat] = new List<CombineInstance>();
            }

            CombineInstance combineInstance = new CombineInstance
            {
                mesh = meshFilter.sharedMesh,
                transform = meshFilter.transform.localToWorldMatrix
            };

            materialToCombine[mat].Add(combineInstance);

            meshFilter.gameObject.hideFlags = HideFlags.HideAndDontSave;
            DestroyImmediate(meshFilter.gameObject);
        }

        // Combine all groups into a single mesh with multiple submeshes
        List<CombineInstance> finalCombine = new();
        List<Material> finalMaterials = new();

        foreach (var kvp in materialToCombine)
        {
            Mesh subMesh = new Mesh();
            subMesh.CombineMeshes(kvp.Value.ToArray(), true, true);

            CombineInstance ci = new CombineInstance
            {
                mesh = subMesh,
                transform = Matrix4x4.identity
            };

            finalCombine.Add(ci);
            finalMaterials.Add(kvp.Key);
        }

        Mesh finalMesh = new Mesh();
        finalMesh.name = $"{datName}-{parent.name}";
        finalMesh.CombineMeshes(finalCombine.ToArray(), false, false);

        var parentMeshFilter = parent.GetComponent<MeshFilter>();
        var parentMeshRenderer = parent.GetComponent<MeshRenderer>();
        if (finalMesh.subMeshCount == 0)
        {
            if (parentMeshFilter != null)
            {
                parentMeshFilter.hideFlags = HideFlags.HideAndDontSave;
                DestroyImmediate(parentMeshFilter);
            }

            if (parentMeshRenderer != null)
            {
                parentMeshRenderer.hideFlags = HideFlags.HideAndDontSave;
                DestroyImmediate(parentMeshRenderer);
            }
        }
        else
        {
            parentMeshFilter ??= parent.AddComponent<MeshFilter>();
            parentMeshRenderer ??= parent.AddComponent<MeshRenderer>();

            // Apply to parent
            parentMeshFilter.sharedMesh = finalMesh;
            parentMeshRenderer.sharedMaterials = finalMaterials.ToArray();
        }
    }

    private static List<GameObject> CreateBSPObject(string datName, BSPModel bspModel, List<WorldObjectModel> worldObjectModels, Transform parentTransform)
    {
        var matchingWorldObjectModel = worldObjectModels.FirstOrDefault(x => x.Name == bspModel.WorldName);
        if (bspModel.WorldName != "PhysicsBSP" && matchingWorldObjectModel == null)
        {
            Debug.LogError($"BSP has no matching WorldObject: {bspModel.WorldName}");
        }

        var name = bspModel.WorldName;
        var bspObject = new GameObject(name);
        bspObject.transform.parent = parentTransform;
        var bspComponent = bspObject.AddComponent<BSPObjectComponent>();
        bspComponent.WorldObjectName = bspModel.WorldName;
        bspComponent.WorldObjectType = matchingWorldObjectModel == null ? WorldObjectTypes.Unknown : matchingWorldObjectModel.WorldObjectType;
        bspObject.isStatic = true;
        SetBSPTag(bspObject, bspModel, matchingWorldObjectModel);
        bspObject.AddComponent<MeshFilter>();
        bspObject.AddComponent<MeshRenderer>().sharedMaterial = MissingMaterial;

        List<GameObject> gameObjects = new List<GameObject>();
        if (CreateBSPChildMeshes(bspModel, bspObject, matchingWorldObjectModel))
        {
            // When this is PhysicsBSP, break into child groupings.
            var children = GetChildren(bspObject);
            foreach (var child in children)
            {
                CombineMeshesPreserveMaterials(datName, child);
            }
            gameObjects.AddRange(children);
        }
        else
        {
            CombineMeshesPreserveMaterials(datName, bspObject);
            gameObjects.Add(bspObject);
        }

        if (ShouldCollide(matchingWorldObjectModel))
        {
            AddColliders(bspObject);
        }

        return gameObjects;
    }

    private static void SetBSPTag(GameObject bspObject, BSPModel bspModel, WorldObjectModel matchingWorldObjectModel)
    {
        if (bspModel.WorldName == "PhysicsBSP" || matchingWorldObjectModel == null || matchingWorldObjectModel.Rayhit)
        {
            return;
        }

        bspObject.tag = LithtechTags.NoRayCast;
    }

    private static bool ShouldCollide(WorldObjectModel matchingWorldObjectModel)
    {
        // TTTT
        return true;
    }

    private static bool ShouldHideObjectType(WorldObjectTypes type)
    {
        return type == WorldObjectTypes.InvisibleVolume ||
            type == WorldObjectTypes.AIRail ||
            type == WorldObjectTypes.AIBarrier ||
            type == WorldObjectTypes.BuyZone ||
            type == WorldObjectTypes.RescueZone ||
            type == WorldObjectTypes.SoftLandingZone;
    }

    private static List<GameObject> GetChildren(GameObject parent, string excludeName = null)
    {
        var children = new List<GameObject>();
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i).gameObject;
            if (child.name != excludeName)
            {
                children.Add(child);
            }
        }
        
        return children.OrderBy(x => x.name).ToList();
    }

    private static void GroupBSPObjects(GameObject rootBSPObject, Dictionary<string, GameObject> worldObjectLookups)
    {
        var bspObjects = GetChildren(rootBSPObject, excludeName:"PhysicsBSP");

        var groups = bspObjects.GroupBy(x =>
        {
            var bspComponent = x.GetComponent<BSPObjectComponent>();
            return bspComponent?.WorldObjectType.ToString() ?? x.name;
        });

        foreach (var grp in groups.OrderBy(x => x.Key))
        {
            GameObject parentGroupObject = new GameObject(grp.Key);
            parentGroupObject.transform.parent = rootBSPObject.transform;

            foreach (GameObject obj in grp)
            {
                obj.transform.parent = parentGroupObject.transform;

                var bspComponent = obj.GetComponent<BSPObjectComponent>();

                if (worldObjectLookups.TryGetValue(bspComponent.WorldObjectName, out var worldObjectGameObject))
                {
                    // Move the matching WorldObject to be a child of the BSP object.
                    // The BSP has the mesh while the WorldObject has the properties about that object.
                    worldObjectGameObject.transform.parent = obj.transform;
                }
                else
                {
                    Debug.LogError($"Found BSP with no matching WorldObject: {obj.name}");
                }
            }

            if (ShouldHideObjectType(WorldObjectModelReader.GetWorldObjectType(grp.Key)))
            {
                parentGroupObject.SetActive(false);
            }
        }
    }

    private static void GroupWorldObjects(GameObject rootWorldObject)
    {
        var worldObjectGameObjects = GetChildren(rootWorldObject);

        var groups = worldObjectGameObjects.GroupBy(x =>
        {
            var worldObjectComponent = x.GetComponent<WorldObjectComponent>();
            return worldObjectComponent?.WorldObjectType.ToString() ?? x.name;
        });

        foreach (var grp in groups.OrderBy(x => x.Key))
        {
            GameObject parentGroupObject = new GameObject(grp.Key);
            parentGroupObject.transform.parent = rootWorldObject.transform;

            foreach (GameObject obj in grp)
            {
                obj.transform.parent = parentGroupObject.transform;
            }

            if (ShouldHideObjectType(WorldObjectModelReader.GetWorldObjectType(grp.Key)))
            {
                parentGroupObject.SetActive(false);
            }
        }
    }

    private static void GroupObjects(GameObject rootBSPObject, GameObject rootWorldObject, Dictionary<string, GameObject> worldObjectLookups)
    {
        GroupBSPObjects(rootBSPObject, worldObjectLookups);
        GroupWorldObjects(rootWorldObject);
    }

    private static List<GameObject> CreateBSPObjects(GameObject rootBSPObject, string name, DATModel datModel)
    {
        try
        {
            List<GameObject> gameObjects = new List<GameObject>();
            int i = 0;
            foreach (var bspModel in datModel.BSPModels.OrderBy(x => x.WorldName))
            {
                i++;
                float progress = (float)i / datModel.BSPModels.Count;
                EditorUtility.DisplayProgressBar($"Creating BSP objects for {name}", $"Item {i} of {datModel.BSPModels.Count}", progress);

                if (bspModel.WorldName.Contains("VisBSP", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                gameObjects.AddRange(CreateBSPObject(name, bspModel, datModel.WorldObjects, rootBSPObject.transform));
            }

            EditorUtility.DisplayProgressBar($"Grouping final BSP objects for {name}", $"Item {i} of {datModel.BSPModels.Count}", 99.9f);
            
            return gameObjects;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void AddWorldObjectComponent(GameObject gameObject, WorldObjectModel worldObjectModel)
    {
        var component = gameObject.AddComponent<WorldObjectComponent>();
        component.ObjectType = worldObjectModel.ObjectType;
        component.WorldObjectType = worldObjectModel.WorldObjectType;
        component.SkyObjectName = worldObjectModel.SkyObjectName;
        component.Position = worldObjectModel.Position.Value;
        component.RotationInDegrees = worldObjectModel.RotationInDegrees.Value;
        component.HasGravity = worldObjectModel.HasGravity ?? false;
        component.MoveToFloor = worldObjectModel.MoveToFloor ?? false;
        component.WeaponType = worldObjectModel.WeaponType;
        component.Scale = worldObjectModel.Scale ?? 1f;
        component.HasSurfaceAlpha = worldObjectModel.SurfaceAlpha.HasValue;
        component.SurfaceAlpha = worldObjectModel.SurfaceAlpha ?? 0;
        component.Filename = worldObjectModel.Filename;
        component.IsABC = worldObjectModel.IsABC;
        component.Skin = worldObjectModel.Skin;
        component.Index = worldObjectModel.Index ?? 0;
        component.Solid = worldObjectModel.Solid;
        component.Visible = worldObjectModel.Visible;
        component.Hidden = worldObjectModel.Hidden;
        component.Rayhit = worldObjectModel.Rayhit;
        component.Shadow = worldObjectModel.Shadow;
        component.Transparent = worldObjectModel.Transparent;
        component.ShowSurface = worldObjectModel.ShowSurface;
        component.UseRotation = worldObjectModel.UseRotation;
        component.SpriteSurfaceName = worldObjectModel.SpriteSurfaceName;
        component.SurfaceColor1 = worldObjectModel.SurfaceColor1 ?? Vector3.zero;
        component.SurfaceColor2 = worldObjectModel.SurfaceColor2 ?? Vector3.zero;
        component.Viscosity = worldObjectModel.Viscosity ?? 0;
        component.TeamNumber = worldObjectModel.TeamNumber;
        component.PlayerNumber = worldObjectModel.PlayerNumber;
    }

    private static GameObject CreateABCPrefabFromWorldObject(GameObject prefab, WorldObjectModel worldObjectModel, Transform parentTransform, string tag)
    {
        GameObject abcObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (abcObject == null)
        {
            Debug.Log($"CreateABCPrefabFromWorldObject - abcObject == null. WorldObjectModel={(worldObjectModel == null ? "null worldobjectmodel" : worldObjectModel.Name)}");
        }

        abcObject.name = $"{worldObjectModel.Name} (Type={worldObjectModel.ObjectType} | Model={prefab.name})";
        abcObject.transform.parent = parentTransform;
        AddWorldObjectComponent(abcObject, worldObjectModel);

        abcObject.transform.position = GetABCObjectLocation(worldObjectModel, abcObject);
        abcObject.transform.eulerAngles = worldObjectModel.RotationInDegrees.Value;
        abcObject.tag = tag;
        if (worldObjectModel.Scale.HasValue && worldObjectModel.Scale != 1f && worldObjectModel.Scale != 0f)
        {
            abcObject.transform.localScale = Vector3.one * worldObjectModel.Scale.Value;
        }

        if (worldObjectModel.MoveToFloor ?? false)
        {
            MoveDownToFloor(abcObject);
        }

        return abcObject;
    }

    private static GameObject CreateGenericWorldObject(WorldObjectModel worldObjectModel, Transform parentTransform, string tag)
    {
        GameObject gameObject = new GameObject();
        gameObject.name = worldObjectModel.Name;
        gameObject.transform.parent = parentTransform;
        AddWorldObjectComponent(gameObject, worldObjectModel);

        var worldObjectModelPosition = worldObjectModel.Position.Value * UnityScaleFactor;

        gameObject.transform.position = worldObjectModelPosition + WorldObjectOffset;
        gameObject.transform.eulerAngles = worldObjectModel.RotationInDegrees.Value;
        gameObject.tag = tag;

        return gameObject;
    }

    private static void MoveObjectToGround(GameObject gameObject, RaycastHit hit)
    {
        var hitPosition = new Vector3(gameObject.transform.position.x, hit.point.y, gameObject.transform.position.z);

        gameObject.transform.position = hitPosition;
    }

    private static void MoveDownToFloor(GameObject abcObject)
    {
        Vector3 epsilon = new Vector3(0, float.Epsilon, 0);
        var hits = Physics.RaycastAll(abcObject.transform.position + epsilon, Vector3.down, MoveToFloorRaycastDistance)
            .Where(x => x.collider.tag != LithtechTags.NoRayCast)
            .OrderBy(x => x.distance)
            .ToList();

        if (hits.Count > 0)
        {
            MoveObjectToGround(abcObject, hits[0]);
        }
    }

    private static Vector3 GetABCObjectLocation(WorldObjectModel worldObjectModel, GameObject abcObject)
    {
        var worldObjectModelPosition = worldObjectModel.Position.Value * UnityScaleFactor;
        return worldObjectModelPosition + WorldObjectOffset;
    }

    private static GameObject CreateWorldObjectABCInstance(WorldObjectModel worldObjectModel, Transform parentTransform)
    {
        if (worldObjectModel.SkinsLowercase.Count > 0)
        {
            // Try to find match on ABC model that has a matching skin used by this DAT's world object.
            // The ABC model might be "banner.abc" but have different skins/textures applied.
            var skinnedPrefab = UnityLookups.GetABCPrefab(worldObjectModel.FilenameLowercase, worldObjectModel.AllSkinsPathsLowercase);
            if (skinnedPrefab != null)
            {
                return CreateABCPrefabFromWorldObject(skinnedPrefab, worldObjectModel, parentTransform, LithtechTags.NoRayCast);
            }
        }

        // No Skins.
        var prefab = UnityLookups.GetABCPrefab(worldObjectModel.FilenameLowercase, string.Empty);
        if (prefab != null)
        {
            CreateABCPrefabFromWorldObject(prefab, worldObjectModel, parentTransform, LithtechTags.NoRayCast);
        }

        var ignoreSkinMatchPrefab = UnityLookups.GetFirstMatchingABCPrefab(worldObjectModel.FilenameLowercase);
        if (ignoreSkinMatchPrefab != null)
        {
            CreateABCPrefabFromWorldObject(ignoreSkinMatchPrefab, worldObjectModel, parentTransform, LithtechTags.NoRayCast);
        }

        if (ABCMaps.TryGetValue(worldObjectModel.FilenameLowercase, out var newName))
        {
            var mapMatchPrefab = UnityLookups.GetFirstMatchingABCPrefab(newName);
            if (mapMatchPrefab != null)
            {
                CreateABCPrefabFromWorldObject(mapMatchPrefab, worldObjectModel, parentTransform, LithtechTags.NoRayCast);
            }
        }

        return null;
    }

    private static Color? GetNormalizedColor(Vector3? color)
    {
        if (color == null)
        {
            return null;
        }

        var normalizedColor = Vector3.Normalize(color.Value);
        return new Color(normalizedColor.x, normalizedColor.y, normalizedColor.z);
    }

    private static GameObject CreateWorldObjectLightInstance(WorldObjectModel worldObjectModel, Transform parentTransform)
    {
        var gameObject = CreateGenericWorldObject(worldObjectModel, parentTransform, LithtechTags.NoRayCast);
        var light = gameObject.AddComponent<Light>();

        if (worldObjectModel.ObjectType == "Light" || worldObjectModel.ObjectType == "ObjectLight" || worldObjectModel.ObjectType == "GlowingLight")
        {
            light.shadows = LightShadows.Soft;
            light.range = worldObjectModel.GetFloatPropValue("LightRadius") * UnityScaleFactor ?? light.range;
            light.color = GetNormalizedColor(worldObjectModel.GetVector3PropValue("LightColor")) ?? light.color;
            light.intensity = worldObjectModel.GetFloatPropValue("BrightScale") ?? light.intensity;
        }
        else if (worldObjectModel.ObjectType == "DirLight")
        {
            light.type = LightType.Spot;
            light.shadows = LightShadows.Soft;
            light.range = worldObjectModel.GetFloatPropValue("LightRadius") * UnityScaleFactor ?? light.range;
            light.intensity = worldObjectModel.GetFloatPropValue("BrightScale") ?? light.intensity;
            light.color = GetNormalizedColor(worldObjectModel.GetVector3PropValue("InnerColor")) ?? light.color;

            var fov = worldObjectModel.GetFloatPropValue("FOV");
            light.innerSpotAngle = fov ?? light.innerSpotAngle;
            light.spotAngle = fov ?? light.spotAngle;
        }
        else if (worldObjectModel.ObjectType == "StaticSunLight")
        {
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = worldObjectModel.GetFloatPropValue("BrightScale") ?? light.intensity;
            light.color = GetNormalizedColor(worldObjectModel.GetVector3PropValue("InnerColor")) ?? light.color;
        }

        return gameObject;
    }
    
    private static GameObject CreateWorldObjectSoundInstance(WorldObjectModel worldObjectModel, Transform parentTransform)
    {
        var clip = UnityLookups.GetAudioClip(worldObjectModel.Filename);
        if (clip == null)
        {
            Debug.LogError($"Error creating WorldObject {worldObjectModel.Name} - could not find WAV (filename={worldObjectModel.Filename})");
            return null;
        }

        var gameObject = CreateGenericWorldObject(worldObjectModel, parentTransform, LithtechTags.NoRayCast);
        var audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;

        audioSource.volume = ((float?)worldObjectModel.GetUIntPropValue("Volume")) / 100f ?? 1f;
        audioSource.loop = worldObjectModel.GetBoolPropValue("Loop") ?? false;

        bool isAmbient = worldObjectModel.GetBoolPropValue("Ambient") ?? false;
        if (isAmbient)
        {
            audioSource.spatialize = false;
            audioSource.spatialBlend = 1f;
        }
        else
        {
            audioSource.spatialize = true;
        }

        var outerRadius = worldObjectModel.GetFloatPropValue("OuterRadius");
        if (outerRadius.HasValue)
        {
            audioSource.maxDistance = outerRadius.Value / 75f;

            var volumeControl = gameObject.AddComponent<Volume2D>();
            volumeControl.audioSource = audioSource;
            volumeControl.maxDist = audioSource.maxDistance;
        }

        return gameObject;
    }

    private static Dictionary<string, GameObject> CreateWorldObjects(GameObject rootWorldObject, string name, DATModel datModel)
    {
        int i = 0;
        var s = $"Creating WorldObjects for {name}\r\n";
        Dictionary<string, GameObject> worldObjectLookups = new Dictionary<string, GameObject>();
        foreach (var worldObjectModel in datModel.WorldObjects.OrderBy(x => x.Name))
        {
            i++;
            float progress = (float)i / datModel.WorldObjects.Count;
            EditorUtility.DisplayProgressBar($"Creating World Objects for {name}", $"Item {i} of {datModel.WorldObjects.Count}", progress);

            GameObject worldObjectGameObject = null;

            // Check if the object has an ABC file reference - if so, we don't care about the type - just create the ABC instance and place it.
            if (worldObjectModel.IsABC)
            {
                worldObjectGameObject = CreateWorldObjectABCInstance(worldObjectModel, rootWorldObject.transform);
            }

            // Create an object based on the type
            if (worldObjectGameObject == null)
            {
                switch (worldObjectModel.WorldObjectType)
                {
                    case WorldObjectTypes.Light:
                        worldObjectGameObject = CreateWorldObjectLightInstance(worldObjectModel, rootWorldObject.transform);
                        break;
                    case WorldObjectTypes.Sound:
                        worldObjectGameObject = CreateWorldObjectSoundInstance(worldObjectModel, rootWorldObject.transform);
                        break;
                };
            }

            if (worldObjectGameObject == null)
            {
                worldObjectGameObject = CreateGenericWorldObject(worldObjectModel, rootWorldObject.transform, LithtechTags.NoRayCast);
            }

            worldObjectLookups.Add(worldObjectModel.UniqueName, worldObjectGameObject);

            //ProcessWorldObject(worldObjectModel);
        }

        Debug.Log(s);

        // SetupAmbientLight();

        EditorUtility.ClearProgressBar();

        return worldObjectLookups;
    }

    /// <summary>
    /// Create assets related to DAT models
    /// </summary>
    /// <param name="abcModels"></param>
    /// <param name="datModels"></param>
    /// <param name="materialLookups"></param>
    private static int CreateAssetsFromDATModels(List<DATModel> datModels)
    {
        AssetDatabase.StartAssetEditing();

        foreach (var datModel in datModels)
        {
            string name = Path.GetFileNameWithoutExtension(datModel.Filename);
            //if (name.ToUpper() != "CHATEAUESCAPE")
            //if (name.ToUpper() != "_RESCUEATTHERUINS")
            //{
            //    continue;
            //}

            GameObject datRootObject = new GameObject(name);

            var rootBSPObject = new GameObject("BSP");
            rootBSPObject.transform.parent = datRootObject.transform;
            var bspObjects = CreateBSPObjects(rootBSPObject, name, datModel);

            var rootWorldObject = new GameObject("WorldObjects");
            rootWorldObject.transform.parent = datRootObject.transform;
            var worldObjectLookups = CreateWorldObjects(rootWorldObject, name, datModel);

            GroupObjects(rootBSPObject, rootWorldObject, worldObjectLookups);

            CreateBSPMeshAndPrefab(datRootObject, bspObjects);

            datRootObject.hideFlags = HideFlags.HideAndDontSave;
            DestroyImmediate(datRootObject);
        }

        RefreshAssetDatabase();

        return datModels.Count;
    }
}
