using UnityEngine;
using UnityEngine.Pool;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SwitchbackGames.BulletTracers
{
    // Example weapon script for the demo scene. It fires a raycast and spawns
    // the tracer, smoke and impact effects. Replace this with your own weapon
    // logic and call SpawnVisuals the same way.
    //
    // Input works with either the old Input Manager or the Input System
    // package, whichever the project is set to, so no setup is needed.
    public class HitscanShooter : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Main Camera that is used in your scene.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("This is the empty Muzzle object that you will position at the gun barrel tip")]
        [SerializeField] private Transform muzzlePoint;

        [Header("Weapon Stats")]
        [SerializeField] private float maxRange = 500f;
        [SerializeField] private float fireRate = 0.1f; // Seconds between shots

        [Header("Visual Effects")]
        [SerializeField] private VisualTracer tracerPrefab;
        [SerializeField] private SmokeTrail smokePrefab;
        [Tooltip("This is the bullet impact prefab")]
        [SerializeField] private GameObject impactPrefab;

        [Tooltip("Simple hipfire spread. Can set to 0 in inspector if not required")]
        [SerializeField] private float hipfireSpread = 0.05f;

        private float nextFireTime = 0f;

        [Header("Muzzle Effects")]
        [Tooltip("The slot for the muzzle flash effect")]
        [SerializeField] private ParticleSystem muzzleFlash;

        [Tooltip("The slot for the muzzle sparks effect")]
        [SerializeField] private ParticleSystem sparks;

        [Tooltip("The slot for the smoke puff that appears at the barrel on each shot")]
        [SerializeField] private ParticleSystem muzzleSmoke;

        // Switched on and off at runtime by DemoToggles. Each one skips spawning
        // that effect without touching anything else, so you can see what each
        // part contributes on its own. Leave them all on for normal use.
        [Header("Effect Switches")]
        public bool tracersEnabled = true;
        public bool smokeTrailEnabled = true;
        public bool muzzleFlashEnabled = true;
        public bool sparksEnabled = true;
        public bool muzzleSmokeEnabled = true;

        private ObjectPool<VisualTracer> tracerPool;
        private ObjectPool<SmokeTrail> smokePool;
        private System.Action<SmokeTrail> releaseSmokeAction;
        private System.Action<VisualTracer> releaseTracerAction;

        private void Start()
        {
            // Fallback in case the camera was not assigned in the inspector
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            // Tracers are pooled so firing does not create garbage every shot.
            // Set this up once here, not per shot.
            tracerPool = new ObjectPool<VisualTracer>(
                createFunc: () => Instantiate(tracerPrefab),
                actionOnGet: t => t.gameObject.SetActive(true),
                actionOnRelease: t => t.gameObject.SetActive(false),
                actionOnDestroy: t => Destroy(t.gameObject),
                collectionCheck: true,   // Catches double releases. Turn off for release builds
                defaultCapacity: 20,
                maxSize: 100
            );

            // Cached so it is not rebuilt on every shot
            releaseTracerAction = t => tracerPool.Release(t);

            // Smoke trails are pooled the same way
            smokePool = new ObjectPool<SmokeTrail>(
                createFunc: () => Instantiate(smokePrefab),
                actionOnGet: s => s.gameObject.SetActive(true),
                actionOnRelease: s => s.gameObject.SetActive(false),
                actionOnDestroy: s => Destroy(s.gameObject),
                collectionCheck: true,
                defaultCapacity: 20,
                maxSize: 100
            );

            releaseSmokeAction = s => smokePool.Release(s);
        }

        private void Update()
        {
            // Swap this for your own input handling
            if (GetFireHeld() && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                FireWeapon();
            }
        }

        private void FireWeapon()
        {
            // Muzzle effects play straight away on each shot. These sit on the
            // weapon, so leave them parented to the muzzle in the scene
            if (muzzleFlash != null && muzzleFlashEnabled) muzzleFlash.Play();
            if (sparks != null && sparksEnabled) sparks.Play();
            if (muzzleSmoke != null && muzzleSmokeEnabled) muzzleSmoke.Play();

            // The shot is fired from the camera centre, not the muzzle. The tracer
            // still starts at the muzzle, so aim stays accurate from any position
            Ray baseRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Hipfire spread. Set hipfireSpread to 0 for no spread
            Vector3 randomOffset = Random.insideUnitSphere * hipfireSpread;
            Vector3 scatteredDirection = baseRay.direction + randomOffset;

            Ray scatteredRay = new Ray(baseRay.origin, scatteredDirection);

            Vector3 hitPoint;

            if (Physics.Raycast(scatteredRay, out RaycastHit hitInfo, maxRange))
            {
                hitPoint = hitInfo.point;

                // Impact decal, sitting just off the surface so it does not z-fight
                if (impactPrefab != null)
                {
                    Quaternion impactRotation = Quaternion.LookRotation(hitInfo.normal);
                    Vector3 impactPosition = hitPoint + (hitInfo.normal * 0.01f);
                    GameObject impact = Instantiate(impactPrefab, impactPosition, impactRotation);
                    Destroy(impact, 5f);
                }
            }
            else
            {
                // Nothing was hit, so aim the effects at a point out at max range.
                // This is what gives sky shots their travel distance
                hitPoint = scatteredRay.origin + (scatteredRay.direction * maxRange);
            }

            SpawnVisuals(hitPoint);
        }

        private void SpawnVisuals(Vector3 targetPoint)
        {
            // Take a tracer from the pool and send it to the point the shot landed
            if (tracerPrefab != null && tracersEnabled)
            {
                VisualTracer tracer = tracerPool.Get();
                tracer.transform.position = muzzlePoint.position;
                tracer.transform.rotation = Quaternion.identity;

                // Set this before Initialize. Initialize can release the tracer
                // straight away at point blank range
                tracer.SetReleaseAction(releaseTracerAction);

                tracer.Initialize(muzzlePoint, targetPoint, playerCamera);
            }

            // Smoke trail flies to the same point, just slower than the tracer
            if (smokePrefab != null && smokeTrailEnabled)
            {
                SmokeTrail smoke = smokePool.Get();

                // Set this before Initialize, same reason as the tracer above
                smoke.SetReleaseAction(releaseSmokeAction);

                // Move this to a serialized field if you want it per weapon
                float smokeSpeed = 150f;

                smoke.Initialize(muzzlePoint, targetPoint, smokeSpeed);
            }
        }

        // Unity defines ENABLE_INPUT_SYSTEM automatically when the Input System
        // package is active, so the right version is compiled either way.
        private bool GetFireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse m = Mouse.current;
            return m != null && m.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
    }
}