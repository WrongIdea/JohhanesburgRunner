using UnityEngine;

namespace JoburgRunner
{
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField] Light sun;
        [SerializeField] float cycleDuration = 120f;
        [SerializeField] float startingCyclePosition = 0.25f;

        readonly Color morningSky = new Color(0.85f, 0.58f, 0.38f);
        readonly Color middaySky = new Color(0.58f, 0.78f, 0.95f);
        readonly Color sunsetSky = new Color(0.95f, 0.42f, 0.22f);
        readonly Color nightSky = new Color(0.04f, 0.06f, 0.13f);

        void Update()
        {
            float t = Mathf.Repeat(startingCyclePosition + Time.time / cycleDuration, 1f);
            Color skyColor;
            float intensity;

            if (t < 0.25f)
            {
                float local = t / 0.25f;
                skyColor = Color.Lerp(morningSky, middaySky, local);
                intensity = Mathf.Lerp(0.75f, 1.25f, local);
            }
            else if (t < 0.5f)
            {
                float local = (t - 0.25f) / 0.25f;
                skyColor = Color.Lerp(middaySky, sunsetSky, local);
                intensity = Mathf.Lerp(1.25f, 0.85f, local);
            }
            else if (t < 0.75f)
            {
                float local = (t - 0.5f) / 0.25f;
                skyColor = Color.Lerp(sunsetSky, nightSky, local);
                intensity = Mathf.Lerp(0.85f, 0.25f, local);
            }
            else
            {
                float local = (t - 0.75f) / 0.25f;
                skyColor = Color.Lerp(nightSky, morningSky, local);
                intensity = Mathf.Lerp(0.25f, 0.75f, local);
            }

            Camera.main.backgroundColor = skyColor;
            RenderSettings.ambientLight = Color.Lerp(Color.black, skyColor, 0.55f);

            if (sun != null)
            {
                sun.color = Color.Lerp(new Color(1f, 0.8f, 0.55f), Color.white, intensity);
                sun.intensity = intensity;
                sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(12f, 72f, intensity / 1.25f), -30f, 0f);
            }
        }
    }
}
