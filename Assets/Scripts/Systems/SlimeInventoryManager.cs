using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SCRIPT OPCIONAL: Gestiona el inventario de slimes que has comprado
/// Este script NO es necesario para que la tienda funcione básicamente
/// Pero lo puedes usar si quieres mantener registro de qué slimes tienes
/// </summary>

[System.Serializable]
public class SlimeInventoryItem
{
    public string slimeName;           // "Slime Rosa"
    public int quantity = 0;           // Cuántos tienes (1, 2, 3, etc)
    public int totalSpent = 0;         // Total de cristales gastados en este tipo
}

public class SlimeInventoryManager : MonoBehaviour
{
    public static SlimeInventoryManager Instance { get; private set; }

    [SerializeField] private List<SlimeInventoryItem> inventory = new List<SlimeInventoryItem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeInventory();
    }

    /// <summary>
    /// Inicializa el inventario con los 4 slimes
    /// </summary>
    private void InitializeInventory()
    {
        inventory.Clear();
        inventory.Add(new SlimeInventoryItem { slimeName = "Slime Rosa", quantity = 0, totalSpent = 0 });
        inventory.Add(new SlimeInventoryItem { slimeName = "Slime Planta", quantity = 0, totalSpent = 0 });
        inventory.Add(new SlimeInventoryItem { slimeName = "Slime Fuego", quantity = 0, totalSpent = 0 });
        inventory.Add(new SlimeInventoryItem { slimeName = "Slime Toxico", quantity = 0, totalSpent = 0 });
    }

    /// <summary>
    /// Agrega un slime al inventario cuando lo compras
    /// </summary>
    public void AddSlimeToInventory(int slimeIndex, int cost)
    {
        if (slimeIndex >= 0 && slimeIndex < inventory.Count)
        {
            inventory[slimeIndex].quantity++;
            inventory[slimeIndex].totalSpent += cost;
            Debug.Log($"Agregaste un {inventory[slimeIndex].slimeName}. Total: {inventory[slimeIndex].quantity}");
        }
    }

    /// <summary>
    /// Obtiene la cantidad de un slime específico
    /// </summary>
    public int GetSlimeQuantity(int slimeIndex)
    {
        if (slimeIndex >= 0 && slimeIndex < inventory.Count)
        {
            return inventory[slimeIndex].quantity;
        }
        return 0;
    }

    /// <summary>
    /// Obtiene el total gastado en un slime
    /// </summary>
    public int GetTotalSpent(int slimeIndex)
    {
        if (slimeIndex >= 0 && slimeIndex < inventory.Count)
        {
            return inventory[slimeIndex].totalSpent;
        }
        return 0;
    }

    /// <summary>
    /// Obtiene todo el inventario
    /// </summary>
    public List<SlimeInventoryItem> GetInventory()
    {
        return inventory;
    }

    /// <summary>
    /// Obtiene el total de slimes que tienes
    /// </summary>
    public int GetTotalSlimes()
    {
        int total = 0;
        foreach (var item in inventory)
        {
            total += item.quantity;
        }
        return total;
    }

    /// <summary>
    /// Obtiene el total gastado en toda la tienda
    /// </summary>
    public int GetTotalSpentInShop()
    {
        int total = 0;
        foreach (var item in inventory)
        {
            total += item.totalSpent;
        }
        return total;
    }

    /// <summary>
    /// Imprime el inventario en la consola (para debug)
    /// </summary>
    public void PrintInventory()
    {
        Debug.Log("╔════════════════════════════════════════╗");
        Debug.Log("║     INVENTARIO DE SLIMES             ║");
        Debug.Log("╠════════════════════════════════════════╣");

        foreach (var item in inventory)
        {
            Debug.Log($"║ {item.slimeName}: {item.quantity} (gastado: {item.totalSpent} cristales)");
        }

        Debug.Log("╠════════════════════════════════════════╣");
        Debug.Log($"║ Total slimes: {GetTotalSlimes()}");
        Debug.Log($"║ Total gastado: {GetTotalSpentInShop()} cristales");
        Debug.Log("╚════════════════════════════════════════╝");
    }
}

/*
CÓMO USAR ESTE SCRIPT:
══════════════════════

1. En la escena, crea un GameObject vacío: "InventoryManager"
2. Arrastra este script al GameObject
3. En ShopUI.cs, cuando compres un slime, agrégalo al inventario:

   EJEMPLO (en OnBuyButtonClicked()):
   
   if (SlimeShopData.Instance.BuySlime(currentSelectedIndex))
   {
       Debug.Log($"¡Compraste {currentSelectedSlime.slimeName}!");
       
       // AGREGAR ESTA LÍNEA:
       SlimeInventoryManager.Instance.AddSlimeToInventory(
           currentSelectedIndex, 
           currentSelectedSlime.price
       );
       
       UpdateBuyButton();
   }

4. En cualquier momento, puedes llamar:
   
   SlimeInventoryManager.Instance.PrintInventory();
   
   Para ver en la consola qué slimes tienes.

EJEMPLO DE OUTPUT EN CONSOLA:
╔════════════════════════════════════════╗
║     INVENTARIO DE SLIMES             ║
╠════════════════════════════════════════╣
║ Slime Rosa: 2 (gastado: 600 cristales)
║ Slime Planta: 1 (gastado: 250 cristales)
║ Slime Fuego: 0 (gastado: 0 cristales)
║ Slime Toxico: 1 (gastado: 350 cristales)
╠════════════════════════════════════════╣
║ Total slimes: 4
║ Total gastado: 1200 cristales
╚════════════════════════════════════════╝

*/
