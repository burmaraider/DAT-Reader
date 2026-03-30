using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ABCPrefabLookupUtility : DataExtractor
{
    private static List<float> LODRelativeHeights = new List<float> { 0.6f, 0.3f, 0.1f, 0.01f };

    public static List<string> HumanoidModels = new List<string>();
    public static List<string> ModelsWithNoMaterials = new List<string>();

    private static List<ABCWithSkinModel> GetABCWithSkins(List<ABCModel> abcModels, List<DATModel> datModels)
    {
        try
        {
            EditorUtility.DisplayProgressBar("Getting ABC filenames from DAT files", $"Getting WorldObject models", 0f);
            var worldObjectModels = datModels.SelectMany(
                datModel => datModel.WorldObjects.Where(x => x.IsABC && x.SkinsLowercase.Count > 0))
                .ToList();

            var uniqueWorldObjectModels = worldObjectModels
                .GroupBy(g => new { g.FilenameLowercase, g.AllSkinsPathsLowercase })
                .Select(g => g.First())
                .ToList();

            int i = 0;
            var matchingABCModels = new List<ABCWithSkinModel>();
            foreach (var abcModel in abcModels)
            {
                i++;
                float progress = (float)i / abcModels.Count;
                EditorUtility.DisplayProgressBar(
                    "Matching ABC models to ones used by DAT files",
                    $"Item {i} of {abcModels.Count}",
                    progress);

                var matches = uniqueWorldObjectModels.Where(
                    worldObjectModel => abcModel.RelativePathToABCFileLowercase == worldObjectModel.FilenameLowercase)
                    .ToList();
                if (matches.Any())
                {
                    var abcWithSkinModels = matches.Select(
                        worldModel => new ABCWithSkinModel
                        {
                            ABCModel = abcModel,
                            AllSkinsPathsLowercase = worldModel.AllSkinsPathsLowercase
                        });

                    matchingABCModels.AddRange(abcWithSkinModels);
                }
            }

            // Make them distinct
            var uniqueABCModels = matchingABCModels
                .GroupBy(x => new { x.AllSkinsPathsLowercase, x.ABCModel.RelativePathToABCFileLowercase })
                .Select(g => g.First())
                .ToList();

            var nonUniqueABCNames = uniqueABCModels.GroupBy(x => new { x.ABCModel.Name })
                .Where(x => x.Count() > 1)
                .Select(x => x.First().ABCModel.Name)
                .ToList();

            foreach (var nonUniqueABCName in nonUniqueABCNames)
            {
                int index = 0;
                foreach (var model in uniqueABCModels.Where(x => x.ABCModel.Name == nonUniqueABCName))
                {
                    index++;
                    model.UniqueIndex = index;
                }
            }

            return uniqueABCModels;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static Material[] GetMaterials(int materialCount, List<string> skins)
    {
        var materials = new Material[materialCount];

        int skinCount = skins?.Count ?? 0;
        if (skinCount == 0)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = MissingMaterial;
            }
        }
        else
        {
            for (int i = 0; i < materials.Length; i++)
            {
                string skin = i > skinCount - 1
                    ? skins[skinCount - 1]
                    : skins[i];

                var material = UnityLookups.GetMaterial(skin);
                if (material != null)
                {
                    materials[i] = material;
                }
                else
                {
                    materials[i] = MissingMaterial;
                }
            }
        }

        return materials;
    }

    private static Material[] GetMaterials(int materialCount, Material material)
    {
        var materials = new Material[materialCount];

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = material;
        }

        return materials;
    }

    private static GameObject CreateBones(GameObject parent, Node node, List<Transform> boneList)
    {
        var boneObject = new GameObject(node.Name);
        boneObject.transform.SetParent(parent.transform, true);
        boneObject.transform.position = node.Matrix.Position;
        boneObject.transform.rotation = node.Matrix.Rotation;

        boneList.Add(boneObject.transform);

        foreach (var childNode in node.Children)
        {
            CreateBones(boneObject, childNode, boneList);
        }

        return boneObject;
    }

    private static GameObject CreateSkinnedMesh(Mesh mesh, GameObject parentGameObject, GameObject rootBone, List<Transform> boneList, string justFilenameWithoutExtension, Material[] materials)
    {
        GameObject skinnedMeshGameObject = new GameObject(justFilenameWithoutExtension + "_mesh");
        skinnedMeshGameObject.transform.SetParent(parentGameObject.transform, false);

        SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshGameObject.AddComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.sharedMesh = mesh;
        skinnedMeshRenderer.bones = boneList.ToArray();
        skinnedMeshRenderer.sharedMaterials = materials;
        skinnedMeshRenderer.updateWhenOffscreen = true;
        skinnedMeshRenderer.rootBone = rootBone.transform;

        return skinnedMeshGameObject;
    }

    private static GameObject CreateGameObjectFromABCReference(ABCModel abcModel, Material[] materials, string nameSuffix)
    {
        // The root bone GameObject already exists on abcModel.RootNode.GameObject.
        // Parent the prefab root to it so the skeleton hierarchy is self-contained.
        string justNameWithoutExtension = Path.GetFileNameWithoutExtension(abcModel.Name);
        GameObject parentGameObject = new GameObject(justNameWithoutExtension);

        var boneList = new List<Transform>();
        var skeletonRoot = CreateBones(parentGameObject, abcModel.RootNode, boneList);

        if (abcModel.LODCount > 1)
        {
            var lods = new List<LOD>();
            for (int lodIndex = 0; lodIndex < abcModel.LODCount; lodIndex++)
            {
                if (!UnityLookups.ABCMeshWithLODLookups.TryGetValue((abcModel.RelativePathToABCFileLowercase, lodIndex), out string unityPathAndFilenameToMesh))
                {
                    Debug.LogError($"Could not find mesh for ABCFile: {abcModel.RelativePathToABCFileLowercase} at lod {lodIndex}");
                    return null;
                }

                // Prep the mesh for use in the Skinned Mesh Renderer.
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(unityPathAndFilenameToMesh);
                var skinnedMeshGameObject = CreateSkinnedMesh(mesh, parentGameObject, skeletonRoot, boneList, $"{justNameWithoutExtension}_LOD{lodIndex}", materials);
                var lod = new LOD(LODRelativeHeights[lodIndex], new Renderer[] { skinnedMeshGameObject.GetComponent<SkinnedMeshRenderer>() });
                lods.Add(lod);
            }

            var lodGroup = parentGameObject.AddComponent<LODGroup>();
            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();
        }
        else
        {
            if (!UnityLookups.ABCMeshLookups.TryGetValue(abcModel.RelativePathToABCFileLowercase, out string unityPathAndFilenameToMesh))
            {
                Debug.LogError($"Could not find mesh for ABCFile: {abcModel.RelativePathToABCFileLowercase}");
                return null;
            }

            // Prep the mesh for use in the Skinned Mesh Renderer.
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(unityPathAndFilenameToMesh);
            var skinnedMeshGameObject = CreateSkinnedMesh(mesh, parentGameObject, skeletonRoot, boneList, justNameWithoutExtension, materials);
        }

        CreateAvatar(abcModel.RootNode.IsHumanoid(), parentGameObject, boneList, abcModel, nameSuffix);

        return parentGameObject;
    }

    private static void CreateAvatar(bool isHumanoid, GameObject parentGameObject, List<Transform> boneList, ABCModel abcModel, string nameSuffix)
    {
        Avatar avatar = null;
        if (isHumanoid)
        {
            avatar = ABCHumanAvatarBuilder.Build(parentGameObject, boneList);
            if (avatar is not null)
            {
                HumanoidModels.Add(abcModel.RelativePathToABCFileLowercase);
            }
        }

        if (avatar is null)
        {
            avatar = AvatarBuilder.BuildGenericAvatar(parentGameObject, boneList[0].name);
            if (!avatar.isValid)
            {
                Debug.LogError("Non-Human avatar is invalid. Check that all required bones are present and the hierarchy is correct.");
                avatar = null;
            }
        }

        if (avatar is not null)
        {
            string relativePathOnlyToABC = Path.GetDirectoryName(abcModel.RelativePathToABCFileLowercase);
            string prefabPathAndFilename = Path.Combine(ABCAvatarPath, relativePathOnlyToABC, abcModel.Name + nameSuffix + ".asset");
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPathAndFilename));
            AssetDatabase.CreateAsset(avatar, prefabPathAndFilename);
            //AssetDatabase.SaveAssets();
            //var loadedAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(prefabPathAndFilename);

            var animator = parentGameObject.AddComponent<Animator>();
            animator.avatar = avatar;
        }
    }

    private static void CreateABCPrefabs(List<ABCWithSkinModel> abcWithSkinsModels)
    {
        int i = 0;
        foreach (var abcWithSkinModel in abcWithSkinsModels)
        {
            i++;
            float progress = (float)i / abcWithSkinsModels.Count;
            EditorUtility.DisplayProgressBar("Creating ABC Prefabs with skins", $"Item {i} of {abcWithSkinsModels.Count}", progress);

            Material[] materials = GetMaterials(abcWithSkinModel.ABCModel.PiecesChunk.TotalTextureCount, abcWithSkinModel.GetSkinList());
            var gameObject = CreateGameObjectFromABCReference(abcWithSkinModel.ABCModel, materials, abcWithSkinModel.GetNameSuffix());

            // Save prefab
            string relativePathOnlyToABC = Path.GetDirectoryName(abcWithSkinModel.ABCModel.RelativePathToABCFileLowercase);
            string prefabPathAndFilename = Path.Combine(ABCPrefabPath, relativePathOnlyToABC, abcWithSkinModel.ABCModel.Name + abcWithSkinModel.GetNameSuffix() + ".prefab");
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPathAndFilename));
            
            PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPathAndFilename);
            abcWithSkinModel.UnityPathAndFilenameToPrefab = prefabPathAndFilename;

            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DestroyImmediate(gameObject);
        }

        EditorUtility.ClearProgressBar();
    }

    private static void CreateABCPrefabs(List<ABCWithSameNameMaterialModel> abcWithSameNameMaterialModels)
    {
        int i = 0;
        foreach (var abcWithSameNameMaterialModel in abcWithSameNameMaterialModels)
        {
            i++;
            float progress = (float)i / abcWithSameNameMaterialModels.Count;
            EditorUtility.DisplayProgressBar("Creating ABC Prefabs with matching PNG", $"Item {i} of {abcWithSameNameMaterialModels.Count}", progress);

            Material[] materials = GetMaterials(abcWithSameNameMaterialModel.ABCModel.PiecesChunk.TotalTextureCount, abcWithSameNameMaterialModel.Material);
            var gameObject = CreateGameObjectFromABCReference(abcWithSameNameMaterialModel.ABCModel, materials, string.Empty);

            string relativePathOnlyToABC = Path.GetDirectoryName(abcWithSameNameMaterialModel.ABCModel.RelativePathToABCFileLowercase);
            string prefabPathAndFilename = Path.Combine(ABCPrefabPath, relativePathOnlyToABC, abcWithSameNameMaterialModel.ABCModel.Name + ".prefab");
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPathAndFilename));

            PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPathAndFilename);

            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DestroyImmediate(gameObject);
        }

        EditorUtility.ClearProgressBar();
    }

    private static void CreateABCPrefabs(List<ABCModel> abcWithNoMaterialModels)
    {
        int i = 0;
        foreach (var abcModel in abcWithNoMaterialModels)
        {
            i++;

            ModelsWithNoMaterials.Add(abcModel.RelativePathToABCFileLowercase);

            float progress = (float)i / abcWithNoMaterialModels.Count;
            EditorUtility.DisplayProgressBar("Creating ABC Prefabs with no materials", $"Item {i} of {abcWithNoMaterialModels.Count}", progress);

            Material[] materials = GetMaterials(abcModel.PiecesChunk.TotalTextureCount, MissingMaterial);
            var gameObject = CreateGameObjectFromABCReference(abcModel, materials, string.Empty);

            string relativePathOnlyToABC = Path.GetDirectoryName(abcModel.RelativePathToABCFileLowercase);
            string prefabPathAndFilename = Path.Combine(ABCPrefabPath, relativePathOnlyToABC, abcModel.Name + ".prefab");
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPathAndFilename));

            PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPathAndFilename);

            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DestroyImmediate(gameObject);
        }

        EditorUtility.ClearProgressBar();
    }

    private static void CreateABCPrefabs(ABCReferenceModels abcReferenceModels)
    {
        AssetDatabase.StartAssetEditing();

        CreateABCPrefabs(abcReferenceModels.ABCWithSkinsModels);
        CreateABCPrefabs(abcReferenceModels.ABCWithSameNameMaterialModels);
        CreateABCPrefabs(abcReferenceModels.ABCModelsWithNoReferences);

        RefreshAssetDatabase();
    }

    private static void SetPrefabLookupPaths(ABCReferenceModels abcReferenceModels)
    {
        foreach(var abcWithSkinsModel in abcReferenceModels.ABCWithSkinsModels)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(abcWithSkinsModel.UnityPathAndFilenameToPrefab);
            UnityLookups.ABCPrefabLookups.Add((abcWithSkinsModel.ABCModel.RelativePathToABCFileLowercase, abcWithSkinsModel.AllSkinsPathsLowercase), prefab);
        }

        foreach(var abcWithSameNameMaterial in abcReferenceModels.ABCWithSameNameMaterialModels)
        {
            var path = Path.ChangeExtension(Path.Combine(ABCPrefabPath, abcWithSameNameMaterial.ABCModel.RelativePathToABCFileLowercase), "prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            UnityLookups.ABCPrefabLookups.Add((abcWithSameNameMaterial.ABCModel.RelativePathToABCFileLowercase, string.Empty), prefab);
        }

        foreach (var abcModel in abcReferenceModels.ABCModelsWithNoReferences)
        {
            var path = Path.ChangeExtension(Path.Combine(ABCPrefabPath, abcModel.RelativePathToABCFileLowercase), "prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            UnityLookups.ABCPrefabLookups.Add((abcModel.RelativePathToABCFileLowercase, string.Empty), prefab);
        }
    }

    private static ABCReferenceModels GetABCReferences(List<DATModel> datModels, List<ABCModel> abcModels)
    {
        // Get list of abcModels referenced by a DAT file. The DAT defines the "skins" for the ABC model.
        var abcWithSkinsModels = GetABCWithSkins(abcModels, datModels);
        var abcWithoutSkinsModels = abcModels.Where(
            abcModel => !abcWithSkinsModels.Any(
                abcWithSkinsModel => abcWithSkinsModel.ABCModel.RelativePathToABCFileLowercase == abcModel.RelativePathToABCFileLowercase))
            .ToList();

        // Get list of abcModels that have a matching DTX/PNG by name.
        // For example, If there's a file "cow.abc" that has no reference in any DAT or has no skins defined
        // but there's a "cow.dtx" file, then match those up.
        var abcModelsWithMatchingMaterial = abcWithoutSkinsModels.Select(
            abcModel => new ABCWithSameNameMaterialModel
            {
                ABCModel = abcModel,
                Material = UnityLookups.GetMaterialByName(abcModel.Name)
            })
            .Where(x => x.Material != null)
            .ToList();

        var abcModelsWithNoReferences = abcWithoutSkinsModels.Where(
            abcWithoutSkinsModel => !abcModelsWithMatchingMaterial.Any(
                x => x.ABCModel.Name.ToLower() == abcWithoutSkinsModel.Name.ToLower()))
            .ToList();

        return new ABCReferenceModels
        {
            ABCWithSkinsModels = abcWithSkinsModels,
            ABCWithSameNameMaterialModels = abcModelsWithMatchingMaterial,
            ABCModelsWithNoReferences = abcModelsWithNoReferences
        };
    }

    public static void SetLookups(List<DATModel> datModels, List<ABCModel> abcModels)
    {
        UnityLookups.ABCPrefabLookups.Clear();

        var abcReferenceModels = GetABCReferences(datModels, abcModels);

        CreateABCPrefabs(abcReferenceModels);
        SetPrefabLookupPaths(abcReferenceModels);
    }
}
