using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace JoziGame
{
    [RequireComponent(typeof(CharacterController))]
    public class JoziPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float sprintMultiplier = 1.45f;
        [SerializeField] float lookSensitivity = 0.12f;
        [SerializeField] float gravity = -22f;

        [Header("Camera")]
        [SerializeField] Transform cameraPivot;

        [Header("Mobile UI")]
        [SerializeField] RectTransform joystickKnob;
        [SerializeField] Graphic joystickBase;
        [SerializeField] Button sprintButton;

        CharacterController controller;
        Vector2 moveInput;
        Vector2 lookInput;
        Vector2 joystickOrigin;
        float verticalVelocity;
        float yaw;
        float pitch = 24f;
        bool sprintButtonHeld;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;

            if (sprintButton != null)
            {
                var trigger = sprintButton.gameObject.AddComponent<EventTrigger>();
                AddEvent(trigger, EventTriggerType.PointerDown, _ => sprintButtonHeld = true);
                AddEvent(trigger, EventTriggerType.PointerUp, _ => sprintButtonHeld = false);
                AddEvent(trigger, EventTriggerType.PointerExit, _ => sprintButtonHeld = false);
            }
        }

        void Update()
        {
            ReadDesktopInput();
            Move();
            AimCamera();
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void SetJoystickOrigin(Vector2 origin)
        {
            joystickOrigin = origin;
        }

        public void AddLookInput(Vector2 delta)
        {
            lookInput += delta;
        }

        void ReadDesktopInput()
        {
            Vector2 keyboard = Vector2.zero;
            Keyboard currentKeyboard = Keyboard.current;
            if (currentKeyboard != null)
            {
                keyboard.x = ReadKey(currentKeyboard.dKey, currentKeyboard.rightArrowKey) - ReadKey(currentKeyboard.aKey, currentKeyboard.leftArrowKey);
                keyboard.y = ReadKey(currentKeyboard.wKey, currentKeyboard.upArrowKey) - ReadKey(currentKeyboard.sKey, currentKeyboard.downArrowKey);
            }

            if (keyboard.sqrMagnitude > 0.01f)
            {
                moveInput = Vector2.ClampMagnitude(keyboard, 1f);
            }

            Mouse currentMouse = Mouse.current;
            if (currentMouse != null && currentMouse.rightButton.isPressed)
            {
                lookInput += currentMouse.delta.ReadValue();
            }
        }

        void Move()
        {
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            bool keyboardSprint = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            float speed = (sprintButtonHeld || keyboardSprint) ? moveSpeed * sprintMultiplier : moveSpeed;
            Vector3 horizontal = (right * moveInput.x + forward * moveInput.y) * speed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        void AimCamera()
        {
            if (lookInput.sqrMagnitude > 0.01f)
            {
                yaw += lookInput.x * lookSensitivity;
                pitch -= lookInput.y * lookSensitivity;
                pitch = Mathf.Clamp(pitch, -18f, 58f);
                lookInput = Vector2.zero;
            }

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        static void AddEvent(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        static float ReadKey(KeyControl keyA, KeyControl keyB)
        {
            return (keyA.isPressed || keyB.isPressed) ? 1f : 0f;
        }
    }
}
