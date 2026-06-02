using UnityEngine;

public class SlimeCaptureSystem : MonoBehaviour
{
    [SerializeField] private KeyCode captureKey = KeyCode.E;
    [SerializeField] private float captureRadius = 2f;
    [SerializeField] private LayerMask slimeLayer;

    private void Update()
    {
        if (Input.GetKeyDown(captureKey))
        {
            TryCaptureNearestSlime();
        }
    }

    private void TryCaptureNearestSlime()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, captureRadius, slimeLayer);
        SlimeController nearestSlime = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            SlimeController slime = hit.GetComponent<SlimeController>();
            if (slime == null)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, slime.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSlime = slime;
            }
        }

        if (nearestSlime != null)
        {
            Capture(nearestSlime);
        }
    }

    private void Capture(SlimeController slime)
    {
        FireSlimeAI fireSlime = slime.GetComponent<FireSlimeAI>();
        if (fireSlime != null && fireSlime.ReactToCaptureAttempt(transform))
        {
            return;
        }

        AcidSlimeAI acidSlime = slime.GetComponent<AcidSlimeAI>();
        if (acidSlime != null && acidSlime.ReactToCaptureAttempt(transform))
        {
            return;
        }

        slime.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}
