using UnityEngine;

namespace JoburgRunner.Environment.Decor
{
    /// <summary>
    /// Lightweight visual signal cycle. Uses global time so every pooled traffic
    /// light stays synchronised without a manager or per-instance state.
    /// </summary>
    public sealed class TrafficLightCycle : MonoBehaviour
    {
        [SerializeField] GameObject redLens;
        [SerializeField] GameObject amberLens;
        [SerializeField] GameObject greenLens;
        [SerializeField, Min(0.1f)] float redSeconds = 4f;
        [SerializeField, Min(0.1f)] float amberSeconds = 1f;
        [SerializeField, Min(0.1f)] float greenSeconds = 4f;

        int activeState = -1;

        void OnEnable()
        {
            activeState = -1;
            UpdateSignal();
        }

        void Update()
        {
            UpdateSignal();
        }

        void UpdateSignal()
        {
            float cycle = redSeconds + amberSeconds + greenSeconds + amberSeconds;
            float phase = Mathf.Repeat(Time.time, cycle);
            int state = phase < redSeconds ? 0 :
                        phase < redSeconds + amberSeconds ? 1 :
                        phase < redSeconds + amberSeconds + greenSeconds ? 2 : 1;
            if (state == activeState)
            {
                return;
            }

            activeState = state;
            if (redLens != null) redLens.SetActive(state == 0);
            if (amberLens != null) amberLens.SetActive(state == 1);
            if (greenLens != null) greenLens.SetActive(state == 2);
        }
    }
}
