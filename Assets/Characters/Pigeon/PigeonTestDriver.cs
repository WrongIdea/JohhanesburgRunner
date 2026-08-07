using System.Collections;
using UnityEngine;

namespace JoburgRunner.Characters.Pigeon
{
    /// <summary>
    /// Drives the PigeonAnimationTest scene: gives a few pigeons self-running
    /// roles (ground / flyer / glider / banker / lander) so the scene demonstrates
    /// every clip on play, and maps keyboard shortcuts to poke a selected pigeon.
    /// Editor/desktop test aid only — not shipped with the game.
    /// </summary>
    public sealed class PigeonTestDriver : MonoBehaviour
    {
        [SerializeField] PigeonController groundBird;
        [SerializeField] PigeonController flyerBird;
        [SerializeField] PigeonController gliderBird;
        [SerializeField] PigeonController bankerBird;
        [SerializeField] PigeonController landerBird;
        [SerializeField] PigeonController[] flockBirds;
        [SerializeField] PigeonController selected;

        void Start()
        {
            if (groundBird != null) groundBird.Initialize(groundBird.transform.position);
            if (flyerBird != null) { flyerBird.Initialize(flyerBird.transform.position); StartCoroutine(FlyerLoop(flyerBird)); }
            if (gliderBird != null) { gliderBird.Initialize(gliderBird.transform.position); StartCoroutine(GliderLoop(gliderBird)); }
            if (bankerBird != null) { bankerBird.Initialize(bankerBird.transform.position); StartCoroutine(BankerLoop(bankerBird)); }
            if (landerBird != null) { landerBird.Initialize(landerBird.transform.position); StartCoroutine(LanderLoop(landerBird)); }
            if (flockBirds != null)
                foreach (var b in flockBirds) if (b != null) b.Initialize(b.transform.position);
            if (selected == null) selected = flyerBird != null ? flyerBird : groundBird;
        }

        IEnumerator FlyerLoop(PigeonController p)
        {
            var wait = new WaitForSeconds(0.1f);
            while (true)
            {
                p.TriggerTakeoff(PigeonController.Flight.Fly);
                yield return new WaitForSeconds(4f);
                p.TriggerLanding();
                yield return new WaitForSeconds(3f);
                yield return wait;
            }
        }

        IEnumerator GliderLoop(PigeonController p)
        {
            p.TriggerTakeoff(PigeonController.Flight.Fly);
            yield return new WaitForSeconds(1f);
            while (true)
            {
                p.SetFlightMode(PigeonController.Flight.Glide);
                yield return new WaitForSeconds(3f);
                p.SetFlightMode(PigeonController.Flight.Fly);
                yield return new WaitForSeconds(2f);
            }
        }

        IEnumerator BankerLoop(PigeonController p)
        {
            p.TriggerTakeoff(PigeonController.Flight.Fly);
            yield return new WaitForSeconds(1f);
            while (true)
            {
                p.SetFlightMode(PigeonController.Flight.BankLeft);
                yield return new WaitForSeconds(2f);
                p.SetFlightMode(PigeonController.Flight.Fly);
                yield return new WaitForSeconds(1f);
                p.SetFlightMode(PigeonController.Flight.BankRight);
                yield return new WaitForSeconds(2f);
                p.SetFlightMode(PigeonController.Flight.Fly);
                yield return new WaitForSeconds(1f);
            }
        }

        IEnumerator LanderLoop(PigeonController p)
        {
            while (true)
            {
                p.TriggerTakeoff(PigeonController.Flight.Fly);
                yield return new WaitForSeconds(2.5f);
                p.TriggerLanding();
                yield return new WaitForSeconds(4f);
            }
        }

        void Update()
        {
            if (selected == null) return;
            if (Input.GetKeyDown(KeyCode.Alpha1) && groundBird != null) selected = groundBird;
            if (Input.GetKeyDown(KeyCode.Alpha2) && flyerBird != null) selected = flyerBird;
            if (Input.GetKeyDown(KeyCode.Alpha3) && gliderBird != null) selected = gliderBird;
            if (Input.GetKeyDown(KeyCode.Alpha4) && bankerBird != null) selected = bankerBird;
            if (Input.GetKeyDown(KeyCode.Alpha5) && landerBird != null) selected = landerBird;

            if (Input.GetKeyDown(KeyCode.A)) selected.SetAlert(true);
            if (Input.GetKeyDown(KeyCode.S)) selected.SetAlert(false);
            if (Input.GetKeyDown(KeyCode.T)) selected.TriggerTakeoff(PigeonController.Flight.Fly);
            if (Input.GetKeyDown(KeyCode.F)) selected.SetFlightMode(PigeonController.Flight.Fly);
            if (Input.GetKeyDown(KeyCode.G)) selected.SetFlightMode(PigeonController.Flight.Glide);
            if (Input.GetKeyDown(KeyCode.L)) selected.SetFlightMode(PigeonController.Flight.BankLeft);
            if (Input.GetKeyDown(KeyCode.R)) selected.SetFlightMode(PigeonController.Flight.BankRight);
            if (Input.GetKeyDown(KeyCode.N)) selected.TriggerLanding();
            if (Input.GetKeyDown(KeyCode.X)) selected.TriggerFrightened();
            if (Input.GetKeyDown(KeyCode.H)) selected.TriggerShortHop();
        }

        void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 640, 220),
                "PIGEON TEST  —  select: 1 ground  2 flyer  3 glider  4 banker  5 lander\n" +
                "A alert on   S alert off   T takeoff   F fly   G glide\n" +
                "L bankL   R bankR   N land   X frightened   H shortHop\n" +
                (selected != null ? "selected: " + selected.name : ""));
        }
    }
}
