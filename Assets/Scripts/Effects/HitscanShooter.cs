using UnityEngine;
using UnityEngine.Pool;

public class HitscanShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Main Camera that is used in your scene.")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("This is the empty Muzzle object that you will position at the gun barrel tip")]
    [SerializeField] private Transform muzzlePoint;

    [Header("Weapon Stats")]
    [SerializeField] private float maxRange = 500f;
    [SerializeField] private float fireRate = 0.1f; // Seconds between shots, for testing it can be lowered even further

    [Header("Visual Effects")]
    [SerializeField] private VisualTracer tracerPrefab;
    [SerializeField] private SmokeTrail smokePrefab;
    [Tooltip("This is the bullet impact prefab")]
    [SerializeField] private GameObject impactPrefab;

    [Tooltip("Simple hipfire spread. Can set to 0 in inspector if not required")]
    [SerializeField] private float hipfireSpread = 0.05f; 

    private float nextFireTime = 0f;

    [Tooltip("The slot for the muzzle flash effect")]
    [SerializeField] private ParticleSystem muzzleFlash;

    [Tooltip("The slot for the muzzle sparks effect")]
    [SerializeField] private ParticleSystem sparks;

    private ObjectPool<VisualTracer> tracerPool;

    private void Start()
    {
        // Not necessary, just a fallback in case the camera wasn't assigned in the inspector
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Object pooling, keep this in Start()
        tracerPool = new ObjectPool<VisualTracer>(
            createFunc: () => Instantiate(tracerPrefab),
            actionOnGet: t => t.gameObject.SetActive(true),
            actionOnRelease: t => t.gameObject.SetActive(false),
            actionOnDestroy: t => Destroy(t.gameObject),
            collectionCheck: true,   // keep true if you're testing it (probably no need)
            defaultCapacity: 20,
            maxSize: 100
        );
    }

    private void Update()
    {
        // Simple firing check
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime) // -> change if needed
        {
            nextFireTime = Time.time + fireRate;
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        // We will just play all muzzle effects immediately as a shot is fired
        if (muzzleFlash != null) muzzleFlash.Play();
        if (sparks != null) sparks.Play();

        // This is important - the tracer effect was created and tested only when firing bullets from the camera (and not the physical muzzle)
        Ray baseRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)); // -> change if needed

        // Simple small random 3D offset to the straight direction for hipfire - is only for testing, can be deleted if it's not wanted
        Vector3 randomOffset = Random.insideUnitSphere * hipfireSpread;
        Vector3 scatteredDirection = baseRay.direction + randomOffset;

        // Create a brand new ray using the original camera position, but the new messy direction - hipfire logic, can be deleted
        Ray scatteredRay = new Ray(baseRay.origin, scatteredDirection);

        Vector3 hitPoint;

        // Perform the Hitscan Raycast using the new scattered hipfire ray
        if (Physics.Raycast(scatteredRay, out RaycastHit hitInfo, maxRange)) 
        {
            hitPoint = hitInfo.point; // -> change if needed

            // Spawn the impact decal
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
            // We shot into the sky or distance using the scattered direction
            hitPoint = scatteredRay.origin + (scatteredRay.direction * maxRange);
        }

        // Trigger visual effects
        SpawnVisuals(hitPoint);
    }

    private void SpawnVisuals(Vector3 targetPoint)
    {
        // First, spawn the tracer FROM THE POOL and Initialize it
        if (tracerPrefab != null)
        {
            VisualTracer tracer = tracerPool.Get();
            tracer.transform.position = muzzlePoint.position;
            tracer.transform.rotation = Quaternion.identity;
            tracer.SetReleaseAction(t => tracerPool.Release(t));

            // We pass the exact "targetPoint" from the Raycast directly into Initialize
            tracer.Initialize(muzzlePoint, targetPoint, playerCamera);
        }

        // Secondly, spawn and Initialize the smoke trail effect
        if (smokePrefab != null)
        {
            SmokeTrail smoke = Instantiate(smokePrefab, muzzlePoint.position, Quaternion.identity);

            // Standard hitscan smoke speed (this can be exposed in the inspector as a variable if wanted)
            float smokeSpeed = 150f;

            smoke.Initialize(muzzlePoint, targetPoint, smokeSpeed, playerCamera);
        }
    }
}