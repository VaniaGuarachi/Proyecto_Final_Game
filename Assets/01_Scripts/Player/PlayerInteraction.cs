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
            TryCaptureSlime(nearbySlime);
            return;
        }

        if (nearbyMachine != null)
        {
            ActivateMachine();
            return;
        }

        Debug.Log("No hay objetos cercanos para interactuar.");
    }

    /// <summary>
    /// Intenta capturar el slime. Los slimes con ReactToCaptureAttempt (Cold, Cleaner)
    /// reaccionan atacando defensivamente cuando el jugador presiona E cerca.
    /// </summary>
    private void TryCaptureSlime(GameObject slimeObj)
    {
        bool reacted = false;

        // Slimes que implementan comportamiento defensivo al ser capturados
        ColdSlimeAI    coldAI    = slimeObj.GetComponent<ColdSlimeAI>();
        CleanerSlimeAI cleanerAI = slimeObj.GetComponent<CleanerSlimeAI>();

        if (coldAI    != null) reacted = coldAI.ReactToCaptureAttempt(transform)    || reacted;
        if (cleanerAI != null) reacted = cleanerAI.ReactToCaptureAttempt(transform) || reacted;

        if (reacted)
            Debug.Log($"[PlayerInteraction] {slimeObj.name} reacciono al intento de captura.");
        else
            Debug.Log($"[PlayerInteraction] Interactuando con: {slimeObj.name}");
    }

    public void FeedSlime()
    {
        if (nearbySlime == null)
        {
            Debug.Log("No hay slimes cercanos para alimentar.");
            return;
        }

        SlimeController sc = nearbySlime.GetComponent<SlimeController>();
        if (sc != null)
        {
            sc.Feed(20, 15);
            Debug.Log($"[PlayerInteraction] Slime alimentado: {nearbySlime.name}");
        }
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
