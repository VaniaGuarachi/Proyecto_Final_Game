using UnityEngine;

public class ShopTrigger : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private KeyCode shopKey = KeyCode.T;  // Tecla para abrir tienda
    [SerializeField] private ShopUI shopUI;                // Referencia al UI de la tienda

    private bool playerInShopZone = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verificar si quien entra es el jugador
        if (collision.CompareTag("Player"))
        {
            playerInShopZone = true;
            Debug.Log("¡Entraste a la zona de la tienda! Presiona T para abrir");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Verificar si quien sale es el jugador
        if (collision.CompareTag("Player"))
        {
            playerInShopZone = false;
            Debug.Log("Saliste de la zona de la tienda");
        }
    }

    private void Update()
    {
        // Si el jugador está en la zona y presiona T
        if (playerInShopZone && Input.GetKeyDown(shopKey))
        {
            OpenShop();
        }
    }

    private void OpenShop()
    {
        if (shopUI != null)
        {
            shopUI.OpenShop();
        }
        else
        {
            Debug.LogError("¡El ShopUI no está asignado en el Inspector!");
        }
    }
}
