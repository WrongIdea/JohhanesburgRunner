using System.Collections.Generic;
using JoburgRunner.Audio;
using JoburgRunner.Obstacles;
using JoburgRunner.Progression;
using JoburgRunner.VFX;
using UnityEditor;
using UnityEngine;

namespace JoburgRunner.Editor
{
    /// <summary>
    /// Project-wide content validation: scans the data-driven ScriptableObjects
    /// for the classic authoring mistakes (missing prefabs/icons/clips,
    /// duplicate IDs, invalid mission targets, unsurvivable obstacle patterns)
    /// and reports them as a single console summary. Run from the menu before a
    /// build.
    /// </summary>
    public static class ContentValidator
    {
        [MenuItem("Joburg Runner/Validate Content")]
        public static void Validate()
        {
            var issues = new List<string>();

            ValidateIds(FindAll<MissionDefinition>(), m => m.id, "Mission", issues);
            ValidateIds(FindAll<AchievementDefinition>(), a => a.id, "Achievement", issues);
            ValidateIds(FindAll<ObstacleDefinition>(), o => o.name, "Obstacle", issues);
            ValidateIds(FindAll<CharacterDefinition>(), c => c.id, "Character", issues);
            ValidateIds(FindAll<CosmeticDefinition>(), c => c.id, "Cosmetic", issues);
            ValidateIds(FindAll<AudioDefinition>(), a => a.id, "Audio", issues);

            foreach (MissionDefinition m in FindAll<MissionDefinition>())
            {
                if (m.targetAmount <= 0) issues.Add($"Mission '{m.id}' has invalid target {m.targetAmount}.");
                if (string.IsNullOrEmpty(m.title)) issues.Add($"Mission '{m.id}' has no title.");
            }

            foreach (ObstacleDefinition o in FindAll<ObstacleDefinition>())
            {
                if (o.prefab == null && o.movement == MovementBehaviour.Static)
                    issues.Add($"Obstacle '{o.name}' is Static but has no prefab.");
            }

            foreach (ObstaclePattern p in FindAll<ObstaclePattern>())
            {
                if (!p.IsSurvivable()) issues.Add($"Obstacle pattern '{p.name}' is NOT survivable.");
            }

            foreach (VFXDefinition v in FindAll<VFXDefinition>())
            {
                if (v.prefab == null && v.sound == null && !v.cameraFeedback)
                    issues.Add($"VFX '{v.name}' has no prefab, sound, or camera feedback — it does nothing.");
            }

            foreach (AudioDefinition a in FindAll<AudioDefinition>())
            {
                if (a.clips == null || a.clips.Length == 0) issues.Add($"Audio '{a.id}' has no clips.");
            }

            foreach (Environment.ZoneCatalog cat in FindAll<Environment.ZoneCatalog>())
            {
                if (cat.zones == null || cat.zones.Length == 0) issues.Add($"ZoneCatalog '{cat.name}' has no zones.");
                else foreach (var z in cat.zones)
                    if (z == null) issues.Add($"ZoneCatalog '{cat.name}' has a null zone reference.");
            }

            if (issues.Count == 0)
            {
                Debug.Log("Content validation passed — no issues found.");
            }
            else
            {
                Debug.LogWarning($"Content validation found {issues.Count} issue(s):\n - " + string.Join("\n - ", issues));
            }
        }

        static void ValidateIds<T>(List<T> items, System.Func<T, string> idOf, string label, List<string> issues) where T : Object
        {
            var seen = new HashSet<string>();
            foreach (T item in items)
            {
                string id = idOf(item);
                if (string.IsNullOrEmpty(id))
                {
                    issues.Add($"{label} '{item.name}' has an empty id.");
                }
                else if (!seen.Add(id))
                {
                    issues.Add($"{label} duplicate id '{id}'.");
                }
            }
        }

        static List<T> FindAll<T>() where T : Object
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }
    }
}
