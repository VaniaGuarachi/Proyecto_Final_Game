using UnityEngine;

public class SlimeEvolutionSystem : MonoBehaviour
{
    [SerializeField] private string requiredMaterial = "Cristales";
    [SerializeField] private int requiredAmount = 3;

    public bool CanEvolve(SlimeController slime)
    {
        return slime != null && ResourceManager.Instance != null &&
               ResourceManager.Instance.HasResource(requiredMaterial, requiredAmount);
    }

    public void TryEvolve(SlimeController slime)
    {
        if (!CanEvolve(slime))
        {
            return;
        }

        ResourceManager.Instance.SpendResource(requiredMaterial, requiredAmount);
        Evolve(slime);
    }

    private void Evolve(SlimeController slime)
    {
        Debug.Log($"{slime.SlimeName} esta listo para evolucionar.");
    }
}
