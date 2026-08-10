using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using JoburgRunner.Environment.Decor;

namespace JoburgRunner.Editor
{
    [InitializeOnLoad]
    public static class JacarandaPrefabBuilder
    {
        const string ModelFolder = "Assets/Art/Environment/Jacaranda";
        const string PrefabFolder = "Assets/Prefabs/Environment/Jacaranda";
        const string ProfilePath = "Assets/Environment/Decor/District_JacarandaAvenue.asset";

        static JacarandaPrefabBuilder()
        {
            EditorApplication.delayCall += CreateWhenReady;
        }

        [MenuItem("Jozi Runner/Assets/Create Jacaranda Prefabs")]
        public static void CreatePrefabs()
        {
            EnsureFolder("Assets/Prefabs/Environment");
            EnsureFolder(PrefabFolder);

            Create(
                $"{ModelFolder}/JacarandaTree_Left.fbx",
                $"{PrefabFolder}/PF_JacarandaTree_Left.prefab",
                "PF_JacarandaTree_Left");
            Create(
                $"{ModelFolder}/JacarandaTree_Right.fbx",
                $"{PrefabFolder}/PF_JacarandaTree_Right.prefab",
                "PF_JacarandaTree_Right");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created isolated Jacaranda prefabs in {PrefabFolder}. They were not added to a scene or spawn set.");
        }

        static void CreateWhenReady()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/JacarandaTree_Left.fbx") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/JacarandaTree_Right.fbx") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/JacarandaTree_Left_LOD1.fbx") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/JacarandaTree_Right_LOD1.fbx") == null)
            {
                return;
            }

            CreatePrefabs();
            InstallAvenue();
        }

        [MenuItem("Jozi Runner/Assets/Install Jacaranda Avenue")]
        public static void InstallAvenue()
        {
            GameObject left = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabFolder}/PF_JacarandaTree_Left.prefab");
            GameObject right = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabFolder}/PF_JacarandaTree_Right.prefab");
            if (left == null || right == null)
            {
                CreatePrefabs();
                left = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabFolder}/PF_JacarandaTree_Left.prefab");
                right = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{PrefabFolder}/PF_JacarandaTree_Right.prefab");
            }

            DistrictDecorProfile profile =
                AssetDatabase.LoadAssetAtPath<DistrictDecorProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DistrictDecorProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.districtName = "Jacaranda Avenue";
            profile.rules = new[]
            {
                new DecorRule
                {
                    socketType = DecorSocketType.Tree,
                    spawnProbability = 1f,
                    maxCountPerSegment = 3,
                    minSpacing = 14f,
                    maxSpacing = 22f,
                    allowedPrefabs = new[] { left, right },
                    sideMatchedPrefabPair = true,
                    allowConsecutiveRepetition = true,
                    scaleRange = new Vector2(0.95f, 1.02f),
                    yawJitterRange = new Vector2(180f, 180f),
                    positionJitter = new Vector2(0.15f, 0.35f),
                    sidewalkOffset = -2f,
                    tintVariation = 0f,
                    useJacarandaPalette = true,
                    requiresIntersection = false,
                    collisionRadius = 1f,
                }
            };
            EditorUtility.SetDirty(profile);

            EnvironmentDecorDirector director =
                Object.FindFirstObjectByType<EnvironmentDecorDirector>(FindObjectsInactive.Include);
            if (director != null)
            {
                SerializedObject serialized = new SerializedObject(director);
                serialized.FindProperty("jacarandaAvenueProfile").objectReferenceValue = profile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(director);
                EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
                EditorSceneManager.SaveScene(director.gameObject.scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Installed 150 m Jacaranda Avenue profile. Main scene spawn cadence is 5 tiles starting near 500 m.");
        }

        static void Create(string modelPath, string prefabPath, string prefabName)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError($"Jacaranda model is not imported: {modelPath}");
                return;
            }

            GameObject root = new GameObject(prefabName);
            GameObject visual = AddVisual(model, root.transform, "LOD0");
            string lod1Path = modelPath.Replace(".fbx", "_LOD1.fbx");
            GameObject lod1Model = AssetDatabase.LoadAssetAtPath<GameObject>(lod1Path);
            GameObject lod1 = AddVisual(lod1Model, root.transform, "LOD1");

            LODGroup lodGroup = root.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[]
            {
                new LOD(0.35f, visual.GetComponentsInChildren<Renderer>(true)),
                new LOD(0.08f, lod1.GetComponentsInChildren<Renderer>(true)),
                new LOD(0.01f, System.Array.Empty<Renderer>()),
            });
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = true;
            lodGroup.RecalculateBounds();

            CapsuleCollider trunk = root.AddComponent<CapsuleCollider>();
            trunk.direction = 1;
            trunk.center = new Vector3(0f, 1.75f, 0f);
            trunk.height = 3.5f;
            trunk.radius = 0.55f;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        static GameObject AddVisual(GameObject model, Transform parent, string name)
        {
            GameObject visual = Object.Instantiate(model, parent);
            visual.name = name;
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            // The source is authored at 9 m. Stretch only vertically to roughly
            // 10 m so the canopy gains height without widening into the road.
            visual.transform.localScale = new Vector3(1f, 10f / 9f, 1f);
            return visual;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
