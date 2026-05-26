using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Textos de UI")]
    [SerializeField] private Text lifeText;
    [SerializeField] private Text resourcesText;
    [SerializeField] private Text activeCharacterText;
    [SerializeField] private Text currentObjectiveText;

    [Header("Estado inicial")]
    [SerializeField] private int playerLife = 100;
    [SerializeField] private string currentObjective = "Crear una colonia autosustentable";

    private void OnEnable()
    {
        CharacterSwitch.ActiveCharacterChanged += UpdateActiveCharacter;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourcesChanged += UpdateResources;
        }
    }

    private void OnDisable()
    {
        CharacterSwitch.ActiveCharacterChanged -= UpdateActiveCharacter;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourcesChanged -= UpdateResources;
        }
    }

    private void Start()
    {
        UpdateLife(playerLife);
        UpdateResources();
        UpdateObjective(currentObjective);
    }

    public void UpdateLife(int life)
    {
        playerLife = life;

        if (lifeText != null)
        {
            lifeText.text = $"Vida: {playerLife}";
        }
    }

    public void UpdateObjective(string objective)
    {
        currentObjective = objective;

        if (currentObjectiveText != null)
        {
            currentObjectiveText.text = $"Objetivo: {currentObjective}";
        }
    }

    private void UpdateResources()
    {
        if (resourcesText == null || ResourceManager.Instance == null)
        {
            return;
        }

        Dictionary<string, int> resources = ResourceManager.Instance.GetAllResources();
        StringBuilder builder = new StringBuilder();

        foreach (KeyValuePair<string, int> resource in resources)
        {
            builder.AppendLine($"{resource.Key}: {resource.Value}");
        }

        resourcesText.text = builder.ToString();
    }

    private void UpdateActiveCharacter(Transform character)
    {
        if (activeCharacterText != null && character != null)
        {
            activeCharacterText.text = $"Personaje: {character.name}";
        }
    }
}
