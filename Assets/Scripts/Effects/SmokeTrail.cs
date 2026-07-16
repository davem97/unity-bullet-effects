using UnityEngine;

public class SmokeTrail : MonoBehaviour
{
    private Vector3 target;
    private float speed;
    private bool isMoving = false;

    public void Initialize(Transform muzzleTransform, Vector3 end, float smokeSpeed, Camera cam)
    {
        transform.position = muzzleTransform.position;
        target = end;
        speed = smokeSpeed;

        // Rotate to face the target immediately
        Vector3 direction = target - transform.position;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        isMoving = true;
    }

    void LateUpdate()
    {
        if (!isMoving) return;

        // Fly toward the exact hitscan coordinate
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // When we hit the exact coordinate, stop and let the smoke fade naturally
        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            isMoving = false;

            var ps = GetComponent<ParticleSystem>();
            if (ps != null) ps.Stop();

            Destroy(gameObject, 2f); // Give particles 2 seconds to fade before deleting the object
        }
    }
}