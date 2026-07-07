using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Makes a vehicle read as driving rather than gliding: wheel hubs spin to
    /// match the distance actually travelled, and wiper blades sweep the
    /// windshield. Speed is measured from the transform each frame, so it works
    /// whether the vehicle is moved by MovingObstacle or by SceneryTraffic.
    /// </summary>
    public class VehicleMotion : MonoBehaviour
    {
        [SerializeField] Transform[] wheels;
        [SerializeField] float wheelRadius = 0.34f;
        [SerializeField] Transform[] wiperPivots;
        [SerializeField] float wiperSweepDegrees = 60f;
        [SerializeField] float wiperCycleSeconds = 1.2f;

        Vector3 lastPosition;
        float wiperPhase;
        Quaternion[] wiperRestRotations;

        void Start()
        {
            lastPosition = transform.position;
            // Random phase so a row of taxis does not wipe in lockstep.
            wiperPhase = Random.value * Mathf.PI * 2f;

            if (wiperPivots != null)
            {
                wiperRestRotations = new Quaternion[wiperPivots.Length];
                for (int i = 0; i < wiperPivots.Length; i++)
                {
                    if (wiperPivots[i] != null)
                    {
                        wiperRestRotations[i] = wiperPivots[i].localRotation;
                    }
                }
            }
        }

        void Update()
        {
            SpinWheels();
            SweepWipers();
        }

        void SpinWheels()
        {
            Vector3 worldDelta = transform.position - lastPosition;
            lastPosition = transform.position;

            if (wheels == null || wheelRadius < 0.01f)
            {
                return;
            }

            float forwardDistance = transform.InverseTransformDirection(worldDelta).z;
            float degrees = forwardDistance / wheelRadius * Mathf.Rad2Deg;
            foreach (Transform wheel in wheels)
            {
                if (wheel != null)
                {
                    wheel.Rotate(degrees, 0f, 0f, Space.Self);
                }
            }
        }

        void SweepWipers()
        {
            if (wiperPivots == null || wiperRestRotations == null)
            {
                return;
            }

            wiperPhase += Time.deltaTime / wiperCycleSeconds * Mathf.PI * 2f;
            float angle = Mathf.Sin(wiperPhase) * wiperSweepDegrees * 0.5f;
            Quaternion sweep = Quaternion.Euler(0f, 0f, angle);
            for (int i = 0; i < wiperPivots.Length; i++)
            {
                if (wiperPivots[i] != null)
                {
                    wiperPivots[i].localRotation = wiperRestRotations[i] * sweep;
                }
            }
        }
    }
}
