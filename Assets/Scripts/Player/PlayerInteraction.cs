using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private GameObject nearbySlime;
    private GameObject nearbyMachine;

    public bool HasNearbySlime => nearbySlime != null;
    public bool HasNearbyMachine => nearbyMachine != null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Slime"))
        {
            nearbySlime = other.gameObject;
            Debug.Log("Slime cercano detectado: " + other.name);
        }
        else if (other.CompareTag("Machine"))
        {
            nearbyMachine = other.gameObject;
            Debug.Log("Maquina cercana detectada: " + other.name);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == nearbySlime)
        {
            nearbySlime = null;
        }

        if (other.gameObject == nearbyMachine)
        {
            nearbyMachine = null;
        }
    }

    public void Interact()
    {
        if (nearbySlime != null)
        {
            Debug.Log("Interactuando con slime: " + nearbySlime.name);
            return;
        }

        if (nearbyMachine != null)
        {
            ActivateMachine();
            return;
        }

        Debug.Log("No hay objetos cercanos para interactuar.");
    }

    public void FeedSlime()
    {
        if (nearbySlime == null)
        {
            Debug.Log("No hay slimes cercanos para alimentar.");
            return;
        }

        Debug.Log("Alimentando slime: " + nearbySlime.name);
    }

    public void BuildOrPlace()
    {
        Debug.Log("Construir o colocar objeto solicitado.");
    }

    public void ActivateMachine()
    {
        if (nearbyMachine == null)
        {
            Debug.Log("No hay maquinas cercanas para activar.");
            return;
        }

        Debug.Log("Activando maquina: " + nearbyMachine.name);
    }
}
