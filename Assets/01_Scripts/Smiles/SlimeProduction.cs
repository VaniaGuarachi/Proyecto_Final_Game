using UnityEngine;

public class SlimeProduction : MonoBehaviour
{
    [SerializeField] private string baseResource = "Biomasa";
    [SerializeField] private string evolvedResource = "Biomasa";
    [SerializeField] private float baseInterval = 8f;
    [SerializeField] private float evolvedInterval = 5f;
    [SerializeField] private int baseAmount = 1;
    [SerializeField] private int evolvedAmount = 2;

    private float timer;
    private bool evolved;

    private void Update()
    {
        timer += Time.deltaTime;
        float interval = evolved ? evolvedInterval : baseInterval;

        if (timer < interval)
        {
            return;
        }

        timer = 0f;
        Produce();
    }

    public void SetEvolved(bool value)
    {
        evolved = value;
        timer = 0f;
    }

    /// <summary>
    /// Configura los recursos y tiempos desde codigo (llamado por el AI del slime al arrancar).
    /// </summary>
    public void Configure(string baseRes, string evolvedRes, int baseAmt, int evolvedAmt,
        float baseIntv, float evolvedIntv)
    {
        baseResource = baseRes;
        evolvedResource = evolvedRes;
        baseAmount = baseAmt;
        evolvedAmount = evolvedAmt;
        baseInterval = baseIntv;
        evolvedInterval = evolvedIntv;
    }

    private void Produce()
    {
        if (ResourceManager.Instance == null)
        {
            return;
        }

        string resource = evolved ? evolvedResource : baseResource;
        int amount = evolved ? evolvedAmount : baseAmount;
        ResourceManager.Instance.AddResource(resource, amount);

        SlimeGenome genome = SlimeGenomeInstaller.Ensure(gameObject);
        if (genome != null)
        {
            genome.RegistrarProduccion(resource, amount);
        }
    }
}
