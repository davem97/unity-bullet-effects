using UnityEngine;

// Runs its LateUpdate LAST (high execution order) so it reads the camera/muzzle
// position AFTER everything else has moved them this frame — keeps the tracer in sync (this is mostly not needed)
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(LineRenderer))]
public class VisualTracer : MonoBehaviour
{
    // A single bullet tracer: draws a streak from the muzzle toward a target point.
    // Built to be pooled — it hands itself back to a pool instead of self-destroying which is better for performance

    [Header("Tracer Settings")]
    public float tracerLength = 15f;      // visible streak length
    public float visualSpeed = 300f;      // overwritten in Initialize based on distance
    public float minLifetime = 0.10f;     // guaranteed on-screen time
    public float maxLifetime = 3f;        // the safety destroy

    [Header("Close-Range Behavior")]
    [Tooltip("Below this distance, tracer never spawns — other VFX take over the visual aspect")]
    public float pointBlankThreshold = 2f;

    [Header("Close-Range Width")]
    [Tooltip("Above this distance, tracer uses standard width. Below, width smoothly shrinks toward minWidthFactor.")]
    public float widthFullDistance = 10f;
    [Tooltip("At or below this distance, tracer width is fully shrunk. Should be at or below point-blank threshold.")]
    public float widthMinDistance = 2f;
    [Tooltip("Width multiplier at widthMinDistance. 0.3 = tracer is 30% of authored width at closest range.")]
    [Range(0.05f, 1f)] public float minWidthFactor = 0.3f;

    // Authored widths from the LineRenderer, which are sampled at spawn.
    private float baseStartWidth;
    private float baseEndWidth;

    private LineRenderer lineRenderer;
    private Transform muzzle;
    private Vector3 targetPoint;

    private Vector3 tipPosition;          // far end, travels toward target
    private Vector3 originPosition;       // near end
    private bool detached = false;        // bool is basically "has near end left the muzzle?"
    private float timeAlive = 0f;
    private bool isInitialized = false;

    // How this tracer returns itself to the pool. If null, it just destroys instead.
    private System.Action<VisualTracer> releaseAction;
    private float releaseTimer;

    // The spawner calls this to tell the tracer how to return itself to the pool.
    // If it's never set (used without a pool), the tracer just destroys itself instead.
    public void SetReleaseAction(System.Action<VisualTracer> release)
    {
        releaseAction = release;
    }

    // Called at every "end of life" point. Returns to the pool if pooled, else destroys.
    // Goes through all exits through one method means a pooled object is never accidentally destroyed.
    private void ReleaseSelf()
    {
        if (releaseAction != null) releaseAction(this);
        else Destroy(gameObject);   // fallback if not pooled
    }

    void Awake()
    {
        // A tracer is just a 2-point line drawn in world space.
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // Remember the width that was set on the prefab, so we can shrink it
        // for close shots and restore it for far ones.
        baseStartWidth = lineRenderer.startWidth;
        baseEndWidth = lineRenderer.endWidth;
    }

    // Signature matches what HitscanShooter already calls. Camera param is unused
    // in this test version but kept so we don't touch the caller.
    // Because this object is being pooled and reused, Initialize resets ALL state every time, 
    // otherwise a reused tracer would show leftovers from its previous shot.
    public void Initialize(Transform muzzleTransform, Vector3 endPoint, Camera cam)
    {
        muzzle = muzzleTransform;
        targetPoint = endPoint;

        float shotDistance = Vector3.Distance(muzzle.position, targetPoint);

        // Point-blank cutoff: skip the tracer entirely at contact range.Muzzle flash + impact decal are enough on their own that close.
        // Note: this releases immediately, so a pooled tracer goes straight back for reuse. (This could probably be done better)
        if (shotDistance < pointBlankThreshold)
        {
            ReleaseSelf();
            return;
        }

        // Smooth width shrink between widthMinDistance and widthFullDistance.
        // InverseLerp normalizes distance to 0-1; SmoothStep gives an ease-in/out curve
        // so there's no visible corner at either end of the transition.
        float t = Mathf.InverseLerp(widthMinDistance, widthFullDistance, shotDistance);
        float widthFactor = Mathf.SmoothStep(minWidthFactor, 1f, t);

        lineRenderer.startWidth = baseStartWidth * widthFactor;
        lineRenderer.endWidth = baseEndWidth * widthFactor;

        // Adaptive visual speed: aim for a consistent perceptible travel time (~0.12s),
        // clamped so extremely long or short shots still read correctly.
        // The streak travels at this speed, not the bullet's real speed, so it always
        // looks the same no matter how fast the actual projectile is. This might need to be tuned so tracers visually don't appear too slow/fast after a shot has already made an impact. 
        float targetTravelTime = 0.12f;
        float adaptiveSpeed = shotDistance / targetTravelTime;
        visualSpeed = Mathf.Clamp(adaptiveSpeed, 40f, 600f);

        // Both ends start at the muzzle; the tip will travel out from here.
        tipPosition = muzzle.position;
        originPosition = muzzle.position;
        detached = false;
        timeAlive = 0f;
        isInitialized = true;

        lineRenderer.SetPosition(0, muzzle.position);
        lineRenderer.SetPosition(1, muzzle.position);
        lineRenderer.enabled = true;

        // Safety net: even if the tip never reaches the target, the tracer releases
        // after maxLifetime so a pooled object can't get stuck "in use" forever.
        releaseTimer = maxLifetime;
    }

    void LateUpdate()
    {
        if (!isInitialized || muzzle == null) return;

        timeAlive += Time.deltaTime;

        // Count down the safety timer; release if it runs out.
        releaseTimer -= Time.deltaTime;
        if (releaseTimer <= 0f) { ReleaseSelf(); return; }

        // Tip travels toward the target at the fixed visual speed.
        tipPosition = Vector3.MoveTowards(tipPosition, targetPoint, visualSpeed * Time.deltaTime);

        if (!detached)
        {
            // Streak is still growing: the back end stays pinned to the muzzle
            // while the tip pulls away, until the streak reaches its full length.
            originPosition = muzzle.position;

            if (Vector3.Distance(originPosition, tipPosition) >= tracerLength)
                detached = true;
        }
        else
        {
            // Streak is at full length: the back end now trails behind the tip
            // at a fixed distance, so the streak flies downrange as a fixed-length dash.
            Vector3 muzzlePos = muzzle.position;
            Vector3 toTip = tipPosition - muzzlePos;
            float muzzleToTip = toTip.magnitude;

            // Never let the back end sit further back than the muzzle itself
            // (matters on short shots where the tip is close to the muzzle).
            float clampedLength = Mathf.Min(tracerLength, muzzleToTip);

            if (muzzleToTip > 0.001f)
                originPosition = tipPosition - (toTip / muzzleToTip) * clampedLength;
            else
                originPosition = muzzlePos;
        }

        // Push the two computed ends into the line each frame.
        lineRenderer.SetPosition(0, originPosition);
        lineRenderer.SetPosition(1, tipPosition);

        // Release once the tip reaches the target AND it's been visible at least minLifetime
        // (minLifetime guarantees even a super-short shot is seen for a frame or two).
        if (Vector3.Distance(tipPosition, targetPoint) < 0.01f && timeAlive >= minLifetime)
        {
            isInitialized = false;
            ReleaseSelf();
            return;
        }
    }
}