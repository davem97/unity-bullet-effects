using UnityEngine;

namespace SwitchbackGames.BulletTracers
{
    // Smoke trail that flies from the muzzle to the point the shot landed, then
    // stops and lets its particles fade out. Put this on a prefab with a Particle
    // System and spawn it from your weapon script by calling Initialize.
    //
    // Turn off Play On Awake on the Particle System. This script starts it.
    [RequireComponent(typeof(ParticleSystem))]
    public class SmokeTrail : MonoBehaviour
    {
        [Header("Close-Range Behavior")]
        [Tooltip("Shots shorter than this spawn no smoke trail. There is no room for one.")]
        public float pointBlankThreshold = 2f;

        [Header("Fade")]
        [Tooltip("How long the particles are given to fade once the trail arrives. " +
                 "Match this to the particle lifetime or they will be cut off.")]
        public float fadeOutTime = 2f;

        private ParticleSystem particles;

        private Vector3 target;
        private float speed;
        private bool isMoving = false;
        private bool isFading = false;
        private float fadeTimer;

        private System.Action<SmokeTrail> releaseAction;

        // Called by the pool owner so the trail can hand itself back when it is
        // done. Without this it destroys itself instead.
        public void SetReleaseAction(System.Action<SmokeTrail> release)
        {
            releaseAction = release;
        }

        private void ReleaseSelf()
        {
            isMoving = false;
            isFading = false;

            if (releaseAction != null) releaseAction(this);
            else Destroy(gameObject);
        }

        void Awake()
        {
            // Cached once so it is not looked up on every shot
            particles = GetComponent<ParticleSystem>();
        }

        // Call this straight after taking a trail from your pool. Pass the muzzle
        // transform, the point the shot landed, and how fast the trail should travel.
        public void Initialize(Transform muzzleTransform, Vector3 end, float smokeSpeed)
        {
            // Too close for a trail to read as anything, so skip it entirely
            if (Vector3.Distance(muzzleTransform.position, end) < pointBlankThreshold)
            {
                ReleaseSelf();
                return;
            }

            transform.position = muzzleTransform.position;
            target = end;
            speed = smokeSpeed;

            // Point at the target so the particles trail out behind it
            Vector3 direction = target - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // Clear out anything left over from the last shot before starting.
            // Skip the Clear and old particles appear at the new muzzle position
            if (particles != null)
            {
                particles.Clear(true);
                particles.Play(true);
            }

            isMoving = true;
            isFading = false;
        }

        void LateUpdate()
        {
            if (isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, target) < 0.01f)
                {
                    // Arrived. Stop making new particles but leave the existing ones
                    // on screen to fade out
                    isMoving = false;
                    isFading = true;
                    fadeTimer = fadeOutTime;

                    if (particles != null)
                    {
                        particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }

                return;
            }

            if (isFading)
            {
                // Wait for the particles to fade, then go back to the pool
                fadeTimer -= Time.deltaTime;
                if (fadeTimer <= 0f)
                {
                    ReleaseSelf();
                }
            }
        }
    }
}