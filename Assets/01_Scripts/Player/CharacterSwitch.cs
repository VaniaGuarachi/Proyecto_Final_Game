using System;
using UnityEngine;

public class CharacterSwitch : MonoBehaviour
{
    [SerializeField] private GameObject explorador;
    [SerializeField] private GameObject criador;
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;

    public static event Action<Transform> ActiveCharacterChanged;

    public GameObject ActiveCharacter { get; private set; }

    private void Start()
    {
        SetActiveCharacter(explorador);
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            SwitchCharacter();
        }
    }

    private void SwitchCharacter()
    {
        GameObject nextCharacter = ActiveCharacter == explorador ? criador : explorador;
        SetActiveCharacter(nextCharacter);
    }

    private void SetActiveCharacter(GameObject character)
    {
        if (character == null)
        {
            return;
        }

        if (explorador != null)
        {
            explorador.SetActive(character == explorador);
        }

        if (criador != null)
        {
            criador.SetActive(character == criador);
        }

        ActiveCharacter = character;
        ActiveCharacterChanged?.Invoke(ActiveCharacter.transform);
    }
}
