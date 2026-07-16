using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [Header("FPS Testing")]
    [Tooltip("Check this to override the framerate.")]
    public bool enableLimit = true;

    [Tooltip("The exact FPS you want to test (e.g., 30, 60, 120).")]
    public int targetFPS = 60;

    private bool wasLimitedLastFrame = false;
    private int lastTargetFPS = -1;

    void Start()
    {
        ApplyFramerate();
    }

    void Update()
    {
        // This allows you to change the checkbox and slider in the Inspector 
        // while the game is running, so you can test on the fly!
        if (enableLimit != wasLimitedLastFrame || targetFPS != lastTargetFPS)
        {
            ApplyFramerate();
        }
    }

    private void ApplyFramerate()
    {
        if (enableLimit)
        {
            // VSync MUST be 0, otherwise targetFrameRate is ignored
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS;
        }
        else
        {
            // Restore default behavior (-1 means no limit, rely on VSync)
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }

        wasLimitedLastFrame = enableLimit;
        lastTargetFPS = targetFPS;
    }
}