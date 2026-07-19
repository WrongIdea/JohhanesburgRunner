using UnityEngine;

namespace JoburgRunner
{
    /// <summary>
    /// Hoverboard booster wallet and board-skin selection, both persisted in
    /// PlayerPrefs. Boosters are consumables: buying one on the Boards page
    /// adds to the count, double-tapping mid-run spends one to activate the
    /// Hoverboard shield. The selected index picks which board visual the
    /// HoverboardVisual shows while riding.
    /// </summary>
    public static class BoardInventory
    {
        const string OwnedKey = "JoburgRunner.Boards.Owned";
        const string SelectedKey = "JoburgRunner.Boards.Selected";

        public static int OwnedCount => PlayerPrefs.GetInt(OwnedKey, 0);

        public static int SelectedIndex => PlayerPrefs.GetInt(SelectedKey, 0);

        public static void Add(int amount)
        {
            PlayerPrefs.SetInt(OwnedKey, Mathf.Max(0, OwnedCount + amount));
            PlayerPrefs.Save();
        }

        /// <summary>Spends one booster. Returns false when none are owned.</summary>
        public static bool TryConsume()
        {
            int owned = OwnedCount;
            if (owned <= 0)
            {
                return false;
            }

            PlayerPrefs.SetInt(OwnedKey, owned - 1);
            PlayerPrefs.Save();
            return true;
        }

        public static void Select(int index)
        {
            PlayerPrefs.SetInt(SelectedKey, Mathf.Max(0, index));
            PlayerPrefs.Save();
        }
    }
}
