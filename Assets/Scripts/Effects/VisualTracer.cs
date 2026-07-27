using UnityEngine;

// Runs its LateUpdate LAST (high execution order) so it reads the camera/muzzle
// position AFTER everything else has moved them this frame — keeps the tracer in sync
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(LineRenderer))]
public class VisualTracer : MonoBehaviour
{
    [Header("Tracer Settings")]
    public float tracerLength = 15f;
    public float visualSpeed = 300f;
    public float minLifetime = 0.10f;
    public float maxLifetime = 3f;

    [Header("Close-Range Behavior")]
    public float pointBlankThreshold = 2f;

    [Header("Close-Range Width")]
    public float widthFullDistance = 10f;
    public float widthMinDistance = 2f;
    [Range(0.05f, 1f)] public float minWidthFactor = 0.3f;

    private float baseStartWidth;
    private float baseEndWidth;

    private LineRenderer lineRenderer;
    private Transform muzzle;
    private Vector3 targetPoint;

    private Vector3 tipPosition;
    private Vector3 originPosition;
    private bool detached = false;
    private float timeAlive = 0f;
    private bool isInitialized = false;

    private float activeTracerLength;

    private System.Action<VisualTracer> releaseAction;
    private float releaseTimer;

    public void SetReleaseAction(System.Action<VisualTracer> release)
    {
        releaseAction = release;
    }

    private void ReleaseSelf()
    {
        if (releaseAction != null) releaseAction(this);
        else Destroy(gameObject);
    }

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        baseStartWidth = lineRenderer.startWidth;
        baseEndWidth = lineRenderer.endWidth;
    }

    public void Initialize(Transform muzzleTransform, Vector3 endPoint, Camera cam)
    {
        muzzle = muzzleTransform;
        targetPoint = endPoint;

        float shotDistance = Vector3.Distance(muzzle.position, targetPoint);

        if (shotDistance < pointBlankThreshold)
        {
            ReleaseSelf();
            return;
        }

        float t = Mathf.InverseLerp(widthMinDistance, widthFullDistance, shotDistance);
        float widthFactor = Mathf.SmoothStep(minWidthFactor, 1f, t);

        lineRenderer.startWidth = baseStartWidth * widthFactor;
        lineRenderer.endWidth = baseEndWidth * widthFactor;

        float targetTravelTime = 0.12f;
        float adaptiveSpeed = shotDistance / targetTravelTime;
        visualSpeed = Mathf.Clamp(adaptiveSpeed, 40f, 600f);

        // Made new changes. So now We use 40% of the shot distance for the length clamp. 
        // This gives close-range bullets room to exist, but lets long-range bullets reach full size.
        activeTracerLength = Mathf.Min(tracerLength, shotDistance * 0.4f);

        Vector3 direction = (targetPoint - muzzle.position).normalized;

        // The max jump is now capped to just slightly longer than the tracer itself (activeTracerLength + 2f).
        // This means a 50m shot will NEVER spawn 40m away. It will spawn at most ~8m away and travel the remaining 42m.
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
            originPosition = muzzle.position;
        }

        timeAlive = 0f;
        isInitialized = true;

        lineRenderer.SetPosition(0, originPosition);
        lineRenderer.SetPosition(1, tipPosition);
        lineRenderer.enabled = true;

        releaseTimer = maxLifetime;
    }

    // LateUpdate is important
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
                originPosition = muzzle.position;
                if (Vector3.Distance(originPosition, tipPosition) >= activeTracerLength)
                    detached = true;
            }
            else
            {
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
            originPosition = Vector3.MoveTowards(originPosition, targetPoint, (visualSpeed * 1.5f) * Time.deltaTime);
        }

        lineRenderer.SetPosition(0, originPosition);
        lineRenderer.SetPosition(1, tipPosition);

        if (Vector3.Distance(originPosition, targetPoint) < 0.01f && timeAlive >= minLifetime)
        {
            isInitialized = false;
            ReleaseSelf();
            return;
        }
    }
}