using UnityEngine;

namespace SwitchbackGames.BulletTracers
{
    // Draws a bullet tracer as a line that travels from the muzzle to the point
    // the shot landed. Put this on a prefab with a Line Renderer and spawn it
    // from your weapon script by calling Initialize.
    //
    // This runs its LateUpdate last (because of the high execution order) so it
    // reads the camera and muzzle position after everything else has moved them
    // this frame, which keeps the tracer in sync.
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(LineRenderer))]
    public class VisualTracer : MonoBehaviour
    {
        [Header("Tracer Settings")]
        [Tooltip("How long the streak is, in metres.")]
        public float tracerLength = 15f;
        [Tooltip("Set automatically on each shot. The value here is only a starting point.")]
        public float visualSpeed = 300f;
        [Tooltip("Shortest time a tracer can be on screen.")]
        public float minLifetime = 0.10f;
        [Tooltip("Longest time a tracer can be on screen. Tracers that reach their " +
                 "target sooner are removed sooner.")]
        public float maxLifetime = 3f;

        [Header("Close-Range Behavior")]
        [Tooltip("Shots shorter than this spawn no tracer. There is no room for one.")]
        public float pointBlankThreshold = 2f;

        [Header("Close-Range Width")]
        [Tooltip("Distance at which the tracer reaches its full width.")]
        public float widthFullDistance = 10f;
        [Tooltip("Distance at which the tracer is at its thinnest.")]
        public float widthMinDistance = 2f;
        [Tooltip("How thin a close-range tracer gets, as a fraction of full width.")]
        [Range(0.05f, 1f)] public float minWidthFactor = 0.3f;

        [Header("Long-Range Linger")]
        [Tooltip("Shots longer than this travel at Linger Speed instead, so they " +
                 "stay on screen longer the further they go.")]
        public float lingerStartDistance = 50f;
        [Tooltip("Travel speed for long shots. Lower values make tracers linger longer.")]
        public float lingerSpeed = 220f;
        [Tooltip("Never let the tracer render thinner than this many pixels.")]
        public float minScreenPixels = 2f;

        [Header("Spawn Variation")]
        [Tooltip("How far ahead of the muzzle the tail of the tracer can sit. " +
                 "Picked at random per shot so they do not all start from the same point.")]
        public float minMuzzleOffset = 0.3f;
        public float maxMuzzleOffset = 3f;

        private float baseStartWidth;
        private float baseEndWidth;
        private float baseWidthMultiplier;

        private LineRenderer lineRenderer;
        private Transform muzzle;
        private Vector3 targetPoint;

        private Camera viewCamera;
        private float closeRangeWidthFactor = 1f;

        private Vector3 tipPosition;
        private Vector3 originPosition;
        private bool detached = false;
        private float timeAlive = 0f;
        private bool isInitialized = false;

        private float activeTracerLength;

        private float muzzleOffset;
        private Vector3 shotDirection;

        private System.Action<VisualTracer> releaseAction;
        private float releaseTimer;

        // Called by the pool owner so the tracer can hand itself back when it is
        // done. Without this it destroys itself instead.
        public void SetReleaseAction(System.Action<VisualTracer> release)
        {
            releaseAction = release;
        }

        private void ReleaseSelf()
        {
            isInitialized = false;
            if (releaseAction != null) releaseAction(this);
            else Destroy(gameObject);
        }

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            // Store the prefab's width settings so every later change scales them
            // instead of replacing them
            baseStartWidth = lineRenderer.startWidth;
            baseEndWidth = lineRenderer.endWidth;
            baseWidthMultiplier = lineRenderer.widthMultiplier;
        }

        // Call this straight after taking a tracer from your pool. Pass the muzzle
        // transform, the point the shot landed, and the camera the player sees from.
        public void Initialize(Transform muzzleTransform, Vector3 endPoint, Camera cam)
        {
            muzzle = muzzleTransform;
            targetPoint = endPoint;
            viewCamera = cam;

            float shotDistance = Vector3.Distance(muzzle.position, targetPoint);

            if (shotDistance < pointBlankThreshold)
            {
                ReleaseSelf();
                return;
            }

            // Tracers get thinner the closer the target is. This goes through
            // widthMultiplier on purpose. Setting startWidth or endWidth instead
            // would flatten the width curve you shaped on the prefab
            float t = Mathf.InverseLerp(widthMinDistance, widthFullDistance, shotDistance);
            closeRangeWidthFactor = Mathf.SmoothStep(minWidthFactor, 1f, t);
            lineRenderer.widthMultiplier = baseWidthMultiplier * closeRangeWidthFactor;

            if (shotDistance > lingerStartDistance)
            {
                // Long shots use a fixed speed, so the further they go the longer
                // they stay on screen
                visualSpeed = lingerSpeed;
            }
            else
            {
                // Short shots cross in a set time instead. At real bullet speed a
                // close shot would be over before it drew a single frame
                float adaptiveSpeed = shotDistance / 0.12f;
                visualSpeed = Mathf.Clamp(adaptiveSpeed, 40f, 600f);
            }

            // Whichever comes first, the tracer reaching its target or maxLifetime
            float expectedFlightTime = shotDistance / visualSpeed;
            releaseTimer = Mathf.Min(maxLifetime, expectedFlightTime + 0.5f);

            // Keep the streak short on close shots so it fits in the space available,
            // while long shots still get the full length
            activeTracerLength = Mathf.Min(tracerLength, shotDistance * 0.4f);

            Vector3 direction = (targetPoint - muzzle.position).normalized;

            shotDirection = direction;
            muzzleOffset = Random.Range(minMuzzleOffset, maxMuzzleOffset);

            // Tracers start a short way down range rather than exactly at the muzzle,
            // and the amount varies per shot. Capped so they never spawn so far out
            // that they look disconnected from the gun
            float maxHeadStart = Mathf.Min(activeTracerLength + 2f, shotDistance * 0.8f);
            float minHeadStart = Mathf.Min(1f, shotDistance * 0.1f);

            float randomHeadStart = Random.Range(minHeadStart, maxHeadStart);

            tipPosition = muzzle.position + (direction * randomHeadStart);

            if (randomHeadStart >= activeTracerLength)
            {
                detached = true;
                originPosition = tipPosition - (direction * activeTracerLength);
            }
            else
            {
                detached = false;
                originPosition = ResolveTailAnchor();
            }

            timeAlive = 0f;
            isInitialized = true;

            lineRenderer.SetPosition(0, originPosition);
            lineRenderer.SetPosition(1, tipPosition);
            lineRenderer.enabled = true;
        }

        void LateUpdate()
        {
            if (!isInitialized || muzzle == null) return;

            timeAlive += Time.deltaTime;

            releaseTimer -= Time.deltaTime;
            if (releaseTimer <= 0f) { ReleaseSelf(); return; }

            tipPosition = Vector3.MoveTowards(tipPosition, targetPoint, visualSpeed * Time.deltaTime);

            bool tipHitTarget = Vector3.Distance(tipPosition, targetPoint) < 0.01f;

            if (!tipHitTarget)
            {
                if (!detached)
                {
                    // Still growing out of the muzzle, so the tail stays put
                    originPosition = ResolveTailAnchor();
                    if (Vector3.Distance(originPosition, tipPosition) >= activeTracerLength)
                        detached = true;
                }
                else
                {
                    // Full length now, so the tail follows the tip down range
                    Vector3 muzzlePos = muzzle.position;
                    Vector3 toTip = tipPosition - muzzlePos;
                    float muzzleToTip = toTip.magnitude;

                    float clampedLength = Mathf.Min(activeTracerLength, muzzleToTip);

                    if (muzzleToTip > 0.001f)
                        originPosition = tipPosition - (toTip / muzzleToTip) * clampedLength;
                    else
                        originPosition = muzzlePos;
                }
            }
            else
            {
                // Tip has landed, so pull the tail in after it rather than letting
                // the tracer just disappear
                originPosition = Vector3.MoveTowards(originPosition, targetPoint, (visualSpeed * 1.5f) * Time.deltaTime);
            }

            lineRenderer.SetPosition(0, originPosition);
            lineRenderer.SetPosition(1, tipPosition);

            // Keep the line thick enough to still be visible far away. Without this
            // a distant tracer is thinner than a pixel and vanishes before it lands
            lineRenderer.widthMultiplier = ResolveWidthMultiplier();

            if (Vector3.Distance(originPosition, targetPoint) < 0.01f && timeAlive >= minLifetime)
            {
                isInitialized = false;
                ReleaseSelf();
                return;
            }
        }

        // Where the tail sits while the tracer is still joined to the gun. Offset a
        // random amount per shot so tracers do not all start from the same point
        // and look like one solid beam.
        private Vector3 ResolveTailAnchor()
        {
            // Clamped so the tail cannot get ahead of the tip on the first frame
            float maxAllowed = Vector3.Distance(muzzle.position, tipPosition);
            return muzzle.position + shotDirection * Mathf.Min(muzzleOffset, maxAllowed);
        }

        // Works out how wide the line needs to be to hold minScreenPixels on screen
        // at its current distance from the camera.
        private float ResolveWidthMultiplier()
        {
            float closeRange = baseWidthMultiplier * closeRangeWidthFactor;
            if (viewCamera == null) return closeRange;

            float camDist = Vector3.Distance(viewCamera.transform.position, tipPosition);
            float worldUnitsPerPixel = 2f * camDist *
                Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;

            float minWorldWidth = worldUnitsPerPixel * minScreenPixels;

            float distanceScaled = (baseEndWidth > 0.0001f)
                ? baseWidthMultiplier * (minWorldWidth / baseEndWidth)
                : closeRange;

            // Whichever is wider, the close-range width or the distance floor
            return Mathf.Max(closeRange, distanceScaled);
        }
    }
}