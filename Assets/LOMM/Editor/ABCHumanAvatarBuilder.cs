using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Builds a Unity Humanoid Avatar from the ABC skeleton hierarchy.
/// Bone names in the ABC format use a "Bip01 *" biped convention similar
/// to 3ds Max Character Studio, which maps cleanly to Unity's HumanBodyBones.
/// </summary>
public static class ABCHumanAvatarBuilder
{
    // The set of humanName strings that Unity's Mecanim marks as required.
    // Built once from HumanTrait so it always matches the current Unity version.
    private static readonly HashSet<string> RequiredHumanNames = BuildRequiredHumanNames();

    private static HashSet<string> BuildRequiredHumanNames()
    {
        var required = new HashSet<string>(StringComparer.Ordinal);
        string[] boneNames = HumanTrait.BoneName;
        for (int i = 0; i < HumanTrait.BoneCount; i++)
        {
            if (HumanTrait.RequiredBone(i))
                required.Add(boneNames[i]);
        }
        return required;
    }

    // Maps each Unity humanName (HumanBodyBones string) to the ABC bone name.
    // Only the required + optional bones present in this rig are listed.
    // Unmapped custom attachment nodes (Zf*, Zb*, X*, Y*, *_node) are kept in
    // the SkeletonBone array so Unity can resolve the full hierarchy, but they
    // receive no humanName entry.
    private static readonly List<HumanToBipedMap> HumanToBipedSuffix = new()
    {
        // ----- ABC Root -----
        new HumanToBipedMap { ExactMatch = true, HumanName = "Hips", ModelBoneName = "Scene Root" },

        // ----- Core spine -----
        new HumanToBipedMap { HumanName = "Spine",      ModelBoneName = "Spine" },
        new HumanToBipedMap { HumanName = "Chest",      ModelBoneName = "Spine1" },
        new HumanToBipedMap { HumanName = "UpperChest", ModelBoneName = "Spine2" },
        new HumanToBipedMap { HumanName = "Neck",       ModelBoneName = "Neck" },
        new HumanToBipedMap { HumanName = "Head",       ModelBoneName = "Head" },

        // ----- Left leg -----
        new HumanToBipedMap { HumanName = "LeftUpperLeg", ModelBoneName = "L Thigh" },
        new HumanToBipedMap { HumanName = "LeftLowerLeg", ModelBoneName = "L Calf"  },
        new HumanToBipedMap { HumanName = "LeftFoot",     ModelBoneName = "L Foot"  },
        new HumanToBipedMap { HumanName = "LeftToes",     ModelBoneName = "L Toe0"  },

        // ----- Right leg -----
        new HumanToBipedMap { HumanName = "RightUpperLeg", ModelBoneName = "R Thigh" },
        new HumanToBipedMap { HumanName = "RightLowerLeg", ModelBoneName = "R Calf"  },
        new HumanToBipedMap { HumanName = "RightFoot",     ModelBoneName = "R Foot"  },
        new HumanToBipedMap { HumanName = "RightToes",     ModelBoneName = "R Toe0"  },

        // ----- Left arm -----
        new HumanToBipedMap { HumanName = "LeftShoulder",  ModelBoneName = "L Clavicle" },
        new HumanToBipedMap { HumanName = "LeftUpperArm",  ModelBoneName = "L UpperArm" },
        new HumanToBipedMap { HumanName = "LeftLowerArm",  ModelBoneName = "L Forearm"  },
        new HumanToBipedMap { HumanName = "LeftHand",      ModelBoneName = "L Hand"     },

        // ----- Right arm -----
        new HumanToBipedMap { HumanName = "RightShoulder", ModelBoneName = "R Clavicle" },
        new HumanToBipedMap { HumanName = "RightUpperArm", ModelBoneName = "R UpperArm" },
        new HumanToBipedMap { HumanName = "RightLowerArm", ModelBoneName = "R Forearm"  },
        new HumanToBipedMap { HumanName = "RightHand",     ModelBoneName = "R Hand"     },

        // ----- Left fingers -----
        new HumanToBipedMap { HumanName = "LeftThumbProximal",      ModelBoneName = "L Finger0"  },
        new HumanToBipedMap { HumanName = "LeftThumbIntermediate",  ModelBoneName = "L Finger01" },
        new HumanToBipedMap { HumanName = "LeftThumbDistal",        ModelBoneName = "L Finger02" },

        new HumanToBipedMap { HumanName = "LeftIndexProximal",      ModelBoneName = "L Finger1"  },
        new HumanToBipedMap { HumanName = "LeftIndexIntermediate",  ModelBoneName = "L Finger11" },
        new HumanToBipedMap { HumanName = "LeftIndexDistal",        ModelBoneName = "L Finger12" },

        new HumanToBipedMap { HumanName = "LeftMiddleProximal",     ModelBoneName = "L Finger2"  },
        new HumanToBipedMap { HumanName = "LeftMiddleIntermediate", ModelBoneName = "L Finger21" },
        new HumanToBipedMap { HumanName = "LeftMiddleDistal",       ModelBoneName = "L Finger22" },

        new HumanToBipedMap { HumanName = "LeftRingProximal",       ModelBoneName = "L Finger3"  },
        new HumanToBipedMap { HumanName = "LeftRingIntermediate",   ModelBoneName = "L Finger31" },
        new HumanToBipedMap { HumanName = "LeftRingDistal",         ModelBoneName = "L Finger32" },

        new HumanToBipedMap { HumanName = "LeftLittleProximal",     ModelBoneName = "L Finger4"  },
        new HumanToBipedMap { HumanName = "LeftLittleIntermediate", ModelBoneName = "L Finger41" },
        new HumanToBipedMap { HumanName = "LeftLittleDistal",       ModelBoneName = "L Finger42" },

        // ----- Right fingers -----
        new HumanToBipedMap { HumanName = "RightThumbProximal",      ModelBoneName = "R Finger0"  },
        new HumanToBipedMap { HumanName = "RightThumbIntermediate",  ModelBoneName = "R Finger01" },
        new HumanToBipedMap { HumanName = "RightThumbDistal",        ModelBoneName = "R Finger02" },

        new HumanToBipedMap { HumanName = "RightIndexProximal",      ModelBoneName = "R Finger1"  },
        new HumanToBipedMap { HumanName = "RightIndexIntermediate",  ModelBoneName = "R Finger11" },
        new HumanToBipedMap { HumanName = "RightIndexDistal",        ModelBoneName = "R Finger12" },

        new HumanToBipedMap { HumanName = "RightMiddleProximal",     ModelBoneName = "R Finger2"  },
        new HumanToBipedMap { HumanName = "RightMiddleIntermediate", ModelBoneName = "R Finger21" },
        new HumanToBipedMap { HumanName = "RightMiddleDistal",       ModelBoneName = "R Finger22" },

        new HumanToBipedMap { HumanName = "RightRingProximal",       ModelBoneName = "R Finger3"  },
        new HumanToBipedMap { HumanName = "RightRingIntermediate",   ModelBoneName = "R Finger31" },
        new HumanToBipedMap { HumanName = "RightRingDistal",         ModelBoneName = "R Finger32" },

        new HumanToBipedMap { HumanName = "RightLittleProximal",     ModelBoneName = "R Finger4"  },
        new HumanToBipedMap { HumanName = "RightLittleIntermediate", ModelBoneName = "R Finger41" },
        new HumanToBipedMap { HumanName = "RightLittleDistal",       ModelBoneName = "R Finger42" },
    };

    private static HumanDescription BuildHumanDescription(Transform root, List<Transform> bones)
    {
        return new HumanDescription
        {
            skeleton = BuildSkeletonBones(root),
            human = BuildHumanBones(bones),
            upperArmTwist = 0.5f,
            lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f,
            lowerLegTwist = 0.5f,
            armStretch = 0.05f,
            legStretch = 0.05f,
            feetSpacing = 0f,
            hasTranslationDoF = false,
        };
    }

    /// <summary>
    /// Walks the entire transform hierarchy rooted at root and returns a
    /// SkeletonBone entry for every transform. Unity requires every node in
    /// the hierarchy to appear here, not just the mapped bones.
    /// </summary>
    private static SkeletonBone[] BuildSkeletonBones(Transform root)
    {
        var skeletonBones = new List<SkeletonBone>();
        CollectSkeletonBones(root, skeletonBones);
        return skeletonBones.ToArray();
    }

    private static void CollectSkeletonBones(Transform t, List<SkeletonBone> list)
    {
        list.Add(new SkeletonBone
        {
            name = t.name,
            position = t.localPosition,
            rotation = t.localRotation,
            scale = t.localScale,
        });

        foreach (Transform child in t)
        {
            CollectSkeletonBones(child, list);
        }
    }

    /// <summary>
    /// Builds the HumanBone array using the HumanToAbcBone mapping table.
    /// Bones not present in the collected transforms are silently skipped so
    /// that partial rigs still produce a valid (if limited) avatar.
    /// </summary>
    private static HumanBone[] BuildHumanBones(List<Transform> bones)
    {
        var humanBones = new List<HumanBone>();

        foreach (var humanToBipedMap in HumanToBipedSuffix)
        {
            Transform matchingBone = null;
            if (humanToBipedMap.ExactMatch)
            {
                matchingBone = bones.FirstOrDefault(x => x.name.Equals(humanToBipedMap.ModelBoneName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                matchingBone = FindBoneBySuffix(bones, humanToBipedMap.ModelBoneName);
            }

            if (matchingBone is null)
            {
                continue;
            }

            humanBones.Add(new HumanBone
            {
                humanName = humanToBipedMap.HumanName,
                boneName = matchingBone.name,
                limit = new HumanLimit { useDefaultValues = true },
            });
        }

        return humanBones.ToArray();
    }

    private static Transform FindBoneBySuffix(List<Transform> bones, string suffix)
    {
        foreach (var bone in bones)
        {
            if (bone.name.StartsWith("Bip", StringComparison.OrdinalIgnoreCase))
            {
                if (bone.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return bone;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks that every bone Unity marks as required is present in the mapped
    /// HumanBone array. Logs an individual error for each missing bone and returns
    /// false if any are absent so the caller can early-out before hitting AvatarBuilder.
    /// </summary>
    private static bool ValidateRequiredBones(HumanBone[] humanBones)
    {
        var mappedNames = new HashSet<string>(humanBones.Select(b => b.humanName), StringComparer.OrdinalIgnoreCase);

        foreach (string required in RequiredHumanNames)
        {
            if (!mappedNames.Contains(required))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a Humanoid Avatar from all bone Transforms collected during
    /// skeleton creation and attaches it to an Animator on parentGameObject.
    /// Must be called after all bones are parented and positioned, but before
    /// CreateAssetModelPrefab.
    /// </summary>
    /// <param name="parentGameObject">Root of the character hierarchy.</param>
    /// <param name="bones">Flat list of all bone Transforms in hierarchy order.</param>
    /// <returns>The created Avatar, or null if a required bone is missing or validation failed.</returns>
    public static Avatar Build(GameObject parentGameObject, List<Transform> bones)
    {
        HumanDescription description = BuildHumanDescription(parentGameObject.transform, bones);

        if (!ValidateRequiredBones(description.human))
        {
            return null;
        }

        Avatar avatar = AvatarBuilder.BuildHumanAvatar(parentGameObject, description);

        if (!avatar.isValid)
        {
            Debug.LogError("[ABCHumanAvatarBuilder] Avatar is invalid. Check that all required bones are present and the hierarchy is correct.");
            return null;
        }

        if (!avatar.isHuman)
        {
            Debug.LogWarning("[ABCHumanAvatarBuilder] Avatar was created but is not recognised as human.");
        }

        return avatar;
    }
}
