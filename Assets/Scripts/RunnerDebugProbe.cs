using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Temporary diagnostic: dumps the player transform stack, animator state
    /// and game flags to the device log so the disappearing-character bug can
    /// be traced on an installed APK. Remove once the bug is fixed.
    /// </summary>
    public class RunnerDebugProbe : MonoBehaviour
    {
        [SerializeField] float interval = 0.25f;

        Transform leanPivot;
        Transform visual;
        Animator animator;
        Renderer bodyRenderer;
        RollController rollController;
        PlayerDeathVisual deathVisual;
        GameManager gameManager;
        float nextLog;

        void Start()
        {
            leanPivot = transform.Find("ModelLeanPivot");
            animator = GetComponentInChildren<Animator>();
            visual = animator != null ? animator.transform : (leanPivot != null && leanPivot.childCount > 0 ? leanPivot.GetChild(0) : null);
            bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            rollController = GetComponent<RollController>();
            deathVisual = GetComponentInChildren<PlayerDeathVisual>();
            gameManager = FindAnyObjectByType<GameManager>();
            Application.logMessageReceived += OnLog;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                Debug.Log($"PROBE-EXCEPTION {condition} | {stackTrace}");
            }
        }

        void Update()
        {
            if (Time.unscaledTime < nextLog)
            {
                return;
            }

            nextLog = Time.unscaledTime + interval;

            string pivotInfo = leanPivot != null
                ? $"pivotPos={leanPivot.localPosition:F2} pivotRot={leanPivot.localEulerAngles:F1}"
                : "pivot=null";
            string visualInfo = visual != null
                ? $"visPos={visual.localPosition:F2} visRot={visual.localEulerAngles:F1} visScale={visual.localScale:F2}"
                : "vis=null";
            string animInfo = animator != null
                ? $"anim={animator.GetCurrentAnimatorStateInfo(0).shortNameHash} animPos={animator.transform.position:F2}"
                : "anim=null";
            string rendererInfo = bodyRenderer != null
                ? $"visible={bodyRenderer.isVisible} boundsC={bodyRenderer.bounds.center:F2}"
                : "renderer=null";
            string stateInfo =
                $"rolling={(rollController != null && rollController.IsRolling)} " +
                $"dead={(deathVisual != null && deathVisual.IsDead)} " +
                $"running={(gameManager != null && gameManager.IsRunning)} " +
                $"over={(gameManager != null && gameManager.IsGameOver)}";

            Debug.Log($"PROBE root={transform.position:F2} {pivotInfo} {visualInfo} {animInfo} {rendererInfo} {stateInfo}");
        }
    }
}
