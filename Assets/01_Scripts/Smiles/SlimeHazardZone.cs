using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SlimeHazardZone : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.5f;
    [SerializeField] private float tickInterval = 0.8f;
    [SerializeField] private bool dissolvesHardMinerals;
    [SerializeField] private string mineralReward = "MineralDuro";
    [SerializeField] private int mineralRewardAmount = 1;

    private float tickTimer;

    public void Configure(float zoneLifetime, float zoneTickInterval, bool canDissolveHardMinerals, string rewardResource, int rewardAmount)
    {
        lifetime = zoneLifetime;
        tickInterval = zoneTickInterval;
        dissolvesHardMinerals = canDissolveHardMinerals;
        mineralReward = rewardResource;
        mineralRewardAmount = rewardAmount;
    }

    private void Awake()
    {
        Collider2D hazardCollider = GetComponent<Collider2D>();
        hazardCollider.isTrigger = true;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (tickTimer > 0f)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            other.GetComponent<CriadorSlimesController>()?.PlayHurtAnimation();
            tickTimer = tickInterval;
            return;
        }

        if (dissolvesHardMinerals && LooksLikeHardMineral(other.gameObject))
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddResource(mineralReward, mineralRewardAmount);
            }

            other.gameObject.SetActive(false);
            tickTimer = tickInterval;
        }
    }

    private bool LooksLikeHardMineral(GameObject target)
    {
        string objectName = target.name.ToLowerInvariant();
        return objectName.Contains("mineral") ||
               objectName.Contains("cristal") ||
               objectName.Contains("roca") ||
               objectName.Contains("duro");
    }
}
