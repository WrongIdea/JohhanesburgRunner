using System;
using JoburgRunner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JoburgRunner.Editor
{
    public static class PrePublishPolishValidator
    {
        static readonly string[] SituationChunks =
        {
            "Chunk_JoburgTaxiStop", "Chunk_JoburgRoadworks",
            "Chunk_JoburgTaxiRank", "Chunk_JoburgStreetActivity"
        };

        [MenuItem("Joburg Runner/Validate Pre-Publish Polish")]
        public static void ValidateOrThrow()
        {
            foreach (string name in SituationChunks)
            {
                string path = "Assets/Prefabs/Chunks/" + name + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new InvalidOperationException("Missing situation chunk: " + path);
                TrackChunk chunk = prefab.GetComponent<TrackChunk>();
                if (chunk == null || chunk.EntrySafeLanes == 0 || chunk.ExitSafeLanes == 0)
                    throw new InvalidOperationException(name + " has no declared escape route.");
            }

            GameObject coin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GoldCoin.prefab");
            SphereCollider trigger = coin != null ? coin.GetComponent<SphereCollider>() : null;
            if (trigger == null || trigger.radius < .64f)
                throw new InvalidOperationException("Coin collection trigger was accidentally reduced.");

            EditorSceneManager.OpenScene("Assets/Scenes/JoburgEndlessRunner.unity");
            if (UnityEngine.Object.FindAnyObjectByType<PowerUpHudController>() == null)
                throw new InvalidOperationException("Compact power-up HUD is missing from the gameplay scene.");
            if (UnityEngine.Object.FindAnyObjectByType<SafeAreaPanel>() == null)
                throw new InvalidOperationException("Gameplay HUD is not protected by a safe-area panel.");
            ChunkManager manager = UnityEngine.Object.FindAnyObjectByType<ChunkManager>();
            if (manager == null) throw new InvalidOperationException("Chunk manager is missing.");
            SerializedObject serializedManager = new SerializedObject(manager);
            SerializedProperty prefabs = serializedManager.FindProperty("chunkPrefabs");
            for (int i=0;i<prefabs.arraySize;i++)
            {
                TrackChunk chunk = prefabs.GetArrayElementAtIndex(i).objectReferenceValue as TrackChunk;
                if (chunk != null && chunk.name.IndexOf("Pothole", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("Pothole remains in production spawning: " + chunk.name);
            }
            if (serializedManager.FindProperty("spawnDistanceAhead").floatValue < 150f)
                throw new InvalidOperationException("Gameplay chunks activate too close to the visible haze boundary.");

            Debug.Log("[PrePublishPolish] PASS: HUD safe area, coin trigger and four fair Johannesburg situations validated; potholes excluded.");
        }
    }
}
