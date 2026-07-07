using UnityEngine;
using UnityEngine.EventSystems;

namespace JoziGame
{
    public class JoziLookPad : MonoBehaviour, IDragHandler
    {
        [SerializeField] JoziPlayerController player;

        public void OnDrag(PointerEventData eventData)
        {
            if (player != null)
            {
                player.AddLookInput(eventData.delta);
            }
        }
    }
}
