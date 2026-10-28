using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SwitchbackGames.BulletTracers
{
    // Input works with either the old Input Manager or the Input System
    // package, whichever the project is set to, so no setup is needed.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Normal walking speed.")]
        [SerializeField] private float walkSpeed = 5f;

        [Tooltip("Speed when holding Left Shift.")]
        [SerializeField] private float sprintSpeed = 8f;

        [Tooltip("How high the player can jump.")]
        [SerializeField] private float jumpHeight = 1.5f;

        [Header("Physics")]
        [Tooltip("Downward force applied to the player.")]
        [SerializeField] private float gravity = -15f; // Slightly higher than Earth gravity feels better in FPS games

        [Header("References")]
        [Tooltip("Drag the Main Camera here so WASD knows which way we are looking.")]
        [SerializeField] private Transform cameraTransform;

        // Internal state
        private CharacterController controller;
        private Vector3 velocity; // Tracks vertical momentum (falling/jumping)
        private bool isGrounded;

        private void Awake()
        {
            // Automatically grab the CharacterController on this GameObject
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // 1. Ground Check
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                // Apply a small constant downward force when grounded to snap smoothly to slopes/stairs
                velocity.y = -2f;
            }

            // 2. Gather Input
            float x = GetHorizontal();
            float z = GetVertical();
            bool isSprinting = GetSprintHeld();

            // 3. Calculate Horizontal Movement (Camera Relative)
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Flatten the vectors so looking up/down doesn't push the player into the floor/sky
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Calculate the raw horizontal velocity (without Time.deltaTime yet)
            Vector3 horizontalVelocity = ((camRight * x) + (camForward * z)) * currentSpeed;

            // 4. Jumping
            if (GetJumpPressed() && isGrounded)
            {
                // Physics formula for calculating the velocity needed to reach a specific height
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // 5. Vertical Movement (Gravity)
            velocity.y += gravity * Time.deltaTime;

            // 6. Combine and apply a SINGLE .Move() call
            Vector3 finalMovement = horizontalVelocity + velocity;
            controller.Move(finalMovement * Time.deltaTime);
        }

        // Unity defines ENABLE_INPUT_SYSTEM automatically when the Input System
        // package is active, so the right version is compiled either way.

        private float GetHorizontal()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return 0f;

            float value = 0f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) value -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) value += 1f;
            return value;
#else
            return Input.GetAxis("Horizontal");
#endif
        }

        private float GetVertical()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return 0f;

            float value = 0f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) value -= 1f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) value += 1f;
            return value;
#else
            return Input.GetAxis("Vertical");
#endif
        }

        private bool GetSprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && k.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        private bool GetJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && k.spaceKey.wasPressedThisFrame;
#else
            return Input.GetButtonDown("Jump");
#endif
        }
    }
}