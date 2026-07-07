using UnityEngine;
using UnityEngine.EventSystems;

namespace JoziGame
{
    public class JoziMobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] JoziPlayerController player;
        [SerializeField] RectTransform knob;
        [SerializeField] float radius = 80f;

        RectTransform rectTransform;
        Vector2 origin;

        void Awake()
        {
            rectTransform = (RectTransform)transform;
            origin = knob != null ? knob.anchoredPosition : Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (knob == null || player == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);
            knob.anchoredPosition = origin + clamped;
            player.SetMoveInput(clamped / radius);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (knob != null)
            {
                knob.anchoredPosition = origin;
            }

            if (player != null)
            {
                player.SetMoveInput(Vector2.zero);
            }
        }
    }
}
