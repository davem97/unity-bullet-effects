using UnityEngine;

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
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

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
}