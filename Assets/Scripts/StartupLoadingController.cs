using System.Collections;
using JoburgRunner.Environment.Decor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoburgRunner
{
    /// <summary>Blocks the menu until every pooled gameplay system is ready.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class StartupLoadingController : MonoBehaviour
    {
        [SerializeField] GameObject loadingPanel;
        [SerializeField] RectTransform spinner;
        [SerializeField] Image progressFill;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] MenuController menuController;
        [SerializeField, Min(0f)] float minimumDisplaySeconds = 0.45f;

        bool loading = true;

        void Awake()
        {
            if (loadingPanel != null) loadingPanel.SetActive(true);
            if (menuController != null) menuController.enabled = false;
        }

        IEnumerator Start()
        {
            float began = Time.realtimeSinceStartup;
            SetProgress(0.08f, "LOADING JOHANNESBURG");

            // Render the overlay before doing synchronous shader warm-up.
            yield return null;
            yield return null;
            SetProgress(0.22f, "PREPARING VISUALS");
            Shader.WarmupAllShaders();

            SetProgress(0.48f, "LOADING BUILDINGS");
            while (!EnvironmentReady()) yield return null;

            SetProgress(0.70f, "PREPARING ROADS & TRAFFIC");
            while (!RoadAndTrafficReady()) yield return null;

            SetProgress(0.90f, "PREPARING OBSTACLES");
            while (!ObstaclesReady()) yield return null;

            while (Time.realtimeSinceStartup - began < minimumDisplaySeconds) yield return null;
            yield return new WaitForEndOfFrame();
            SetProgress(1f, "READY");
            yield return null;

            loading = false;
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (menuController != null) menuController.enabled = true;
        }

        void Update()
        {
            if (loading && spinner != null)
                spinner.Rotate(0f, 0f, -220f * Time.unscaledDeltaTime);
        }

        static bool EnvironmentReady()
        {
            EnvironmentDecorDirector director = FindAnyObjectByType<EnvironmentDecorDirector>();
            return director == null || director.IsReady;
        }

        static bool RoadAndTrafficReady()
        {
            RoadSegmentSpawner road = FindAnyObjectByType<RoadSegmentSpawner>();
            SceneryTraffic traffic = FindAnyObjectByType<SceneryTraffic>();
            return (road == null || road.IsReady) && (traffic == null || traffic.IsReady);
        }

        static bool ObstaclesReady()
        {
            ChunkManager chunks = FindAnyObjectByType<ChunkManager>();
            return chunks == null || chunks.IsReady;
        }

        void SetProgress(float value, string message)
        {
            if (progressFill != null) progressFill.fillAmount = value;
            if (statusText != null) statusText.text = message;
        }
    }
}
