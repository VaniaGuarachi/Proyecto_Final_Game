using UnityEngine;

public class SlimeFeedingSystem : MonoBehaviour
{
    [SerializeField] private KeyCode feedKey = KeyCode.F;
    [SerializeField] private float feedRadius = 2f;
    [SerializeField] private LayerMask slimeLayer;
    [SerializeField] private int foodCost = 1;
    [SerializeField] private int hungerRecovered = 20;
    [SerializeField] private int happinessGained = 10;

    private void Update()
    {
        if (Input.GetKeyDown(feedKey))
        {
            TryFeedNearbySlime();
        }
    }

    private void TryFeedNearbySlime()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, feedRadius, slimeLayer);
        if (hit == null)
        {
            return;
        }

        SlimeController slime = hit.GetComponent<SlimeController>();
        if (slime == null || ResourceManager.Instance == null)
        {
            return;
        }

        if (ResourceManager.Instance.SpendFood(foodCost))
        {
            slime.Feed(hungerRecovered, happinessGained);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, feedRadius);
    }
}
