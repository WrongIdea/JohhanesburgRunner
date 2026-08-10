using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoziRunner.AssetPipeline.Editor
{
    public static class AssetReviewSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/AssetReviewScene.unity";

        [MenuItem("Jozi Runner/Asset Pipeline/Create or Reset Review Scene")]
        public static void CreateOrReset()
        {
            EnsureSettings();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AssetReviewScene";

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Neutral Ground";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);

            CreateCamera("Gameplay Camera", new Vector3(0f, 4.2f, -8.5f), new Vector3(14f, 0f, 0f), 60f);
            CreateCamera("Front Inspection Camera", new Vector3(0f, 2f, -7f), new Vector3(10f, 0f, 0f), 45f);
            CreateCamera("Side Inspection Camera", new Vector3(7f, 2f, 0f), new Vector3(10f, -90f, 0f), 45f);
            CreateCamera("Rear Inspection Camera", new Vector3(0f, 2f, 7f), new Vector3(10f, 180f, 0f), 45f);

            GameObject light = new GameObject("Neutral Key Light", typeof(Light));
            light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            Light lamp = light.GetComponent<Light>();
            lamp.type = LightType.Directional;
            lamp.intensity = 1.1f;

            GameObject scale = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scale.name = "One Metre Scale Reference";
            scale.transform.position = new Vector3(-2.5f, 0.5f, 0f);
            scale.transform.localScale = Vector3.one;

            GameObject person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            person.name = "Character Height Reference (1.8m)";
            person.transform.position = new Vector3(2.5f, 0.9f, 0f);
            person.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);

            GameObject reviewRoot = new GameObject("REVIEW_MODEL_ROOT");
            reviewRoot.transform.SetSiblingIndex(0);
            new GameObject("Performance Statistics (use Asset Review window)");

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"Created isolated review scene at {ScenePath}. No production scene was changed.");
        }

        static void EnsureSettings()
        {
            const string path = "Assets/Editor/AssetPipeline/AssetPipelineSettings.asset";
            if (AssetDatabase.LoadAssetAtPath<AssetPipelineSettings>(path) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AssetPipelineSettings>(), path);
            AssetDatabase.SaveAssets();
        }

        static void CreateCamera(string name, Vector3 position, Vector3 euler, float fieldOfView)
        {
            GameObject go = new GameObject(name, typeof(Camera));
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            Camera camera = go.GetComponent<Camera>();
            camera.fieldOfView = fieldOfView;
            camera.enabled = name == "Gameplay Camera";
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
        }
    }
}
