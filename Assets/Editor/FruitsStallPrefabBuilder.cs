using System.IO;
using JoburgRunner.Environment.Decor;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>Creates the mobile fruit-stall décor prefab from the approved model.</summary>
    public static class FruitsStallPrefabBuilder
    {
        public const string ModelPath =
            "Assets/Art/GameReady/Props/fruits_stall/fruits-stall-v2_optimized.fbx";
        public const string PrefabPath = "Assets/Prefabs/Decor/DecorFruitsStall.prefab";
        const string WomanPrefabPath = "Assets/Prefabs/Decor/DecorRoadsideWoman.prefab";

        [MenuItem("Jozi Runner/Assets/Create Fruits Stall Prefab")]
        public static GameObject CreatePrefab()
        {
            if (AssetImporter.GetAtPath(ModelPath) is ModelImporter importer &&
                !importer.bakeAxisConversion)
            {
                importer.bakeAxisConversion = true;
                importer.SaveAndReimport();
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError($"Fruit-stall model has not imported: {ModelPath}");
                return null;
            }

            EnsureFolder("Assets/Prefabs/Decor");
            GameObject root = new GameObject("DecorFruitsStall");
            GameObject visual = Object.Instantiate(model, root.transform);
            visual.name = "FruitStallVisual";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Vector3.one;

            // The market vendor is part of the pooled stall group, guaranteeing
            // that she appears and disappears with every stall on either side.
            GameObject womanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WomanPrefabPath);
            GameObject woman = null;
            if (womanPrefab != null)
            {
                woman = Object.Instantiate(womanPrefab, root.transform);
                woman.name = "FruitStallVendor";
                // The stall faces local -Z. A 2.8 m forward move on its 45-degree
                // roadside pose shifts the vendor approximately 2 m inward toward
                // the road and 2 m toward the approaching player.
                woman.transform.localPosition = new Vector3(0f, 0f, -2.8f);
                woman.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                woman.transform.localScale = Vector3.one;
                woman.SetActive(false);

                StallVendorReveal reveal = root.AddComponent<StallVendorReveal>();
                SerializedObject revealData = new SerializedObject(reveal);
                revealData.FindProperty("vendor").objectReferenceValue = woman;
                revealData.FindProperty("revealDelay").floatValue = 1f;
                revealData.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"Fruit-stall vendor prefab has not imported: {WomanPrefabPath}");
            }

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        material.enableInstancing = true;
                    }
                }
            }

            LODGroup lodGroup = root.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[]
            {
                new LOD(0.08f, renderers),
                new LOD(0.01f, System.Array.Empty<Renderer>())
            });
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = true;
            lodGroup.RecalculateBounds();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created mobile fruit-stall prefab: {PrefabPath}");
            return prefab;
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
