using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ABCPrefabLookupUtility : DataExtractor
{
    private static bool TrySetExistingLookups(ABCReferenceModels abcReferenceModels)
    {
        var prefabFilenames = Directory.GetFiles(ABCPrefabPath, "*.prefab", SearchOption.AllDirectories);
        if (prefabFilenames.Length == 0)
        {
            return false;
        }

        // Update UnityPathAndFilenameToPrefab on each model
        foreach (var abcWithSkinModel in abcReferenceModels.ABCWithSkinsModels)
        {
            string name = Path.GetFileNameWithoutExtension(abcWithSkinModel.ABCModel.Name + abcWithSkinModel.GetNameSuffix());
            string relativePathOnlyToABC = Path.GetDirectoryName(abcWithSkinModel.ABCModel.RelativePathToABCFileLowercase);
            string prefabPathAndFilename = Path.Combine(ABCPrefabPath, relativePathOnlyToABC, name + ".prefab");
            abcWithSkinModel.UnityPathAndFilenameToPrefab = prefabPathAndFilename;
        }

        SetPrefabLookupPaths(abcReferenceModels);

        return true;
    }

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

    private static GameObject CreateGameObjectFromABCReference(ABCModel abcModel, Material[] materials)
    {
        if (!UnityLookups.ABCMeshLookups.TryGetValue(abcModel.RelativePathToABCFileLowercase, out string unityPathAndFilenameToMesh))
        {
            Debug.LogError($"Could not find mesh for ABCFile: {abcModel.RelativePathToABCFileLowercase}");
            return null;
        }

        // The root bone GameObject already exists on abcModel.RootNode.GameObject.
        // Parent the prefab root to it so the skeleton hierarchy is self-contained.
        GameObject rootObject = new GameObject(abcModel.Name);

        // Parent the skeleton root to the prefab root.
        abcModel.RootNode.GameObject.transform.SetParent(rootObject.transform, false);

        // Prep the mesh for use in the Skinned Mesh Renderer.
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(unityPathAndFilenameToMesh);
        mesh.RecalculateBounds();

        // Create a "visual" object that will hold the mesh(es).
        GameObject rendererObject = new GameObject(abcModel.Name + "_Visual");
        rendererObject.transform.SetParent(rootObject.transform, false);
        SkinnedMeshRenderer smr = rendererObject.AddComponent<SkinnedMeshRenderer>();
        List<Transform> flattenedBoneTransforms = abcModel.GetFlattenedBoneTransforms();
        smr.bones = flattenedBoneTransforms.ToArray();
        smr.rootBone = rootObject.transform;
        smr.sharedMesh = mesh;
        smr.sharedMaterials = materials;

        if (abcModel.RootNode.IsHumanoid())
        {
            Avatar avatar = AvatarBuilder.BuildGenericAvatar(rootObject, abcModel.RootNode.GameObject.name);
            avatar.name = abcModel.Name + "_Avatar";

            string relativePathOnlyToABC = Path.GetDirectoryName(abcModel.RelativePathToABCFileLowercase);
            string avatarPath = Path.Combine(ABCPrefabPath, relativePathOnlyToABC, avatar.name + ".asset").ConvertFolderSeperators();
            Directory.CreateDirectory(Path.GetDirectoryName(avatarPath));
            AssetDatabase.CreateAsset(avatar, avatarPath);

            Animator animator = rootObject.AddComponent<Animator>();
            animator.avatar = avatar;
        }

        return rootObject;
    }

    private static void CreateABCPrefabs(List<ABCWithSkinModel> abcWithSkinsModels)
    {
        int i = 0;
        foreach (var abcWithSkinModel in abcWithSkinsModels)
        {
            i++;
            float progress = (float)i / abcWithSkinsModels.Count;
            EditorUtility.DisplayProgressBar("Creating ABC Prefabs with skins", $"Item {i} of {abcWithSkinsModels.Count}", progress);

            Material[] materials = GetMaterials(abcWithSkinModel.ABCModel.PiecesChunk.GetTotalTextureCount(), abcWithSkinModel.GetSkinList());
            var gameObject = CreateGameObjectFromABCReference(abcWithSkinModel.ABCModel, materials);

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

            Material[] materials = GetMaterials(abcWithSameNameMaterialModel.ABCModel.PiecesChunk.GetTotalTextureCount(), abcWithSameNameMaterialModel.Material);
            var gameObject = CreateGameObjectFromABCReference(abcWithSameNameMaterialModel.ABCModel, materials);

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
            float progress = (float)i / abcWithNoMaterialModels.Count;
            EditorUtility.DisplayProgressBar("Creating ABC Prefabs with no materials", $"Item {i} of {abcWithNoMaterialModels.Count}", progress);

            Material[] materials = GetMaterials(abcModel.PiecesChunk.GetTotalTextureCount(), MissingMaterial);
            var gameObject = CreateGameObjectFromABCReference(abcModel, materials);

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

    public static void SetLookups(bool alwaysCreate, List<DATModel> datModels, List<ABCModel> abcModels)
    {
        UnityLookups.ABCPrefabLookups.Clear();

        var abcReferenceModels = GetABCReferences(datModels, abcModels);

        if (!alwaysCreate)
        {
            if (TrySetExistingLookups(abcReferenceModels))
            {
                return;
            }
        }

        CreateABCPrefabs(abcReferenceModels);
        SetPrefabLookupPaths(abcReferenceModels);
    }
}
