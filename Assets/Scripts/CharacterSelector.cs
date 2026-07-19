using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Activates the player visual chosen on the ME page and keeps the other
    /// characters disabled. Runs before every other script so anything that
    /// looks up the animator or visual components (all of which ignore
    /// inactive objects) only ever sees the selected character. Changing the
    /// selection reloads the scene — the same pattern the menu and restart
    /// buttons use — so nothing can hold a reference to the old character.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CharacterSelector : MonoBehaviour
    {
        const string PrefKey = "JoburgRunner.Character";

        [SerializeField] GameObject[] characterVisuals;

        public int CharacterCount => characterVisuals != null ? characterVisuals.Length : 0;

        public static int SelectedIndex => PlayerPrefs.GetInt(PrefKey, 0);

        public static void Select(int index)
        {
            PlayerPrefs.SetInt(PrefKey, index);
            PlayerPrefs.Save();
        }

        void Awake()
        {
            if (characterVisuals == null || characterVisuals.Length == 0)
            {
                return;
            }

            int selected = Mathf.Clamp(SelectedIndex, 0, characterVisuals.Length - 1);
            for (int i = 0; i < characterVisuals.Length; i++)
            {
                if (characterVisuals[i] != null)
                {
                    characterVisuals[i].SetActive(i == selected);
                }
            }
        }
    }
}
