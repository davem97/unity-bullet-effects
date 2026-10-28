using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SwitchbackGames.BulletTracers
{
    // Input works with either the old Input Manager or the Input System
    // package, whichever the project is set to, so no setup is needed.
    public class SimpleMouseLook : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Main Camera transform (sibling, not a child).")]
        [SerializeField] private Transform cameraTransform;

        [Tooltip("PlayerTrackingPoint — where the camera should sit each frame.")]
        [SerializeField] private Transform cameraTrackingPoint;

        [Tooltip("Shooter transform — pitches up/down with the camera.")]
        [SerializeField] private Transform weaponPivot;

        [Header("Settings")]
        [SerializeField] private float mouseSensitivity = 100f;
        [SerializeField] private float clampAngle = 89f;

        private float pitch = 0f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // Press Escape to release the cursor, so a build is not a trap
            if (GetUnlockPressed())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Vector2 look = GetLookDelta();

            float mouseX = look.x * mouseSensitivity * Time.deltaTime;
            float mouseY = look.y * mouseSensitivity * Time.deltaTime;

            // Yaw: rotate the Player itself — children (body, shooter, tracking point) go with it.
            transform.Rotate(Vector3.up * mouseX);

            // Pitch: tracked locally, applied to camera + weapon pivot in LateUpdate / here.
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -clampAngle, clampAngle);

            if (weaponPivot != null)
                weaponPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (cameraTransform == null || cameraTrackingPoint == null) return;

            // Camera position tracks PlayerTrackingPoint (which moves with the Player).
            cameraTransform.position = cameraTrackingPoint.position;

            // Camera rotation = player's yaw + our tracked pitch.
            cameraTransform.rotation = Quaternion.Euler(pitch, transform.eulerAngles.y, 0f);
        }

        // Unity defines ENABLE_INPUT_SYSTEM automatically when the Input System
        // package is active, so the right version is compiled either way.

        private Vector2 GetLookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse m = Mouse.current;
            if (m == null) return Vector2.zero;

            // The Input System reports mouse movement in raw pixels, while the old
            // Input Manager scales it down first. The 0.1 brings the two roughly
            // into line so one sensitivity value works for both.
            return m.delta.ReadValue() * 0.1f;
#else
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
        }

        private bool GetUnlockPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            return k != null && k.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}