using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SwitchbackGames.BulletTracers
{
    // Demo script. Enables for switching each effect on and off with the number keys (1-5)
    // while the game is running so you can see what each effect does
    // without having to exit play mode, remove components and press play again.
    // This script is attached to the same object as HitscanShooter, or anywhere in the scene
    // with the shooter assigned.
    // Input works with either the old Input Manager or the Input System
    // package, whichever the project is set to, so no setup is needed.
    public class DemoToggles : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The shooter whose effects these keys switch on and off. " +
                 "Left empty, it looks for one on this object.")]
        [SerializeField] private HitscanShooter shooter;

        [Header("Display")]
        [Tooltip("Shows the current state of each effect on screen.")]
        [SerializeField] private bool showOverlay = true;

        private GUIStyle overlayStyle;

        private void Awake()
        {
            if (shooter == null)
            {
                shooter = GetComponent<HitscanShooter>();
            }
        }

        private void Update()
        {
            if (shooter == null) return;

            if (GetKeyPressed(1)) shooter.smokeTrailEnabled = !shooter.smokeTrailEnabled;
            if (GetKeyPressed(2)) shooter.sparksEnabled = !shooter.sparksEnabled;
            if (GetKeyPressed(3)) shooter.muzzleFlashEnabled = !shooter.muzzleFlashEnabled;
            if (GetKeyPressed(4)) shooter.tracersEnabled = !shooter.tracersEnabled;
            if (GetKeyPressed(5)) shooter.muzzleSmokeEnabled = !shooter.muzzleSmokeEnabled;
        }

        private void OnGUI()
        {
            if (!showOverlay || shooter == null) return;

            if (overlayStyle == null)
            {
                overlayStyle = new GUIStyle(GUI.skin.label);
                overlayStyle.fontSize = 14;
                overlayStyle.normal.textColor = Color.white;
            }

            GUILayout.BeginArea(new Rect(12f, 12f, 260f, 150f));

            GUILayout.Label("1  Smoke trail   " + StateText(shooter.smokeTrailEnabled), overlayStyle);
            GUILayout.Label("2  Muzzle sparks " + StateText(shooter.sparksEnabled), overlayStyle);
            GUILayout.Label("3  Muzzle flash  " + StateText(shooter.muzzleFlashEnabled), overlayStyle);
            GUILayout.Label("4  Bullet tracer " + StateText(shooter.tracersEnabled), overlayStyle);
            GUILayout.Label("5  Muzzle smoke  " + StateText(shooter.muzzleSmokeEnabled), overlayStyle);

            GUILayout.EndArea();
        }

        private string StateText(bool on)
        {
            return on ? "ON" : "OFF";
        }

        // Unity defines ENABLE_INPUT_SYSTEM automatically when the Input System
        // package is active, so the right version is compiled either way.
        private bool GetKeyPressed(int number)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard k = Keyboard.current;
            if (k == null) return false;

            switch (number)
            {
                case 1: return k.digit1Key.wasPressedThisFrame;
                case 2: return k.digit2Key.wasPressedThisFrame;
                case 3: return k.digit3Key.wasPressedThisFrame;
                case 4: return k.digit4Key.wasPressedThisFrame;
                case 5: return k.digit5Key.wasPressedThisFrame;
                default: return false;
            }
#else
            switch (number)
            {
                case 1: return Input.GetKeyDown(KeyCode.Alpha1);
                case 2: return Input.GetKeyDown(KeyCode.Alpha2);
                case 3: return Input.GetKeyDown(KeyCode.Alpha3);
                case 4: return Input.GetKeyDown(KeyCode.Alpha4);
                case 5: return Input.GetKeyDown(KeyCode.Alpha5);
                default: return false;
            }
#endif
        }
    }
}
