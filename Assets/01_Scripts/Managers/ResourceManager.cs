using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Recursos")]
    [SerializeField] private int agua;
    [SerializeField] private int comida;
    [SerializeField] private int biomasa;
    [SerializeField] private int cristales;
    [SerializeField] private int energia;
    [SerializeField] private int mineral;
    [SerializeField] private int esporas;
    [SerializeField] private int calor;
    [SerializeField] private int acido;
    [SerializeField] private int mineralDuro;
    [SerializeField] private int frio;

    public event Action ResourcesChanged;

    public int Agua => agua;
    public int Comida => comida;
    public int Biomasa => biomasa;
    public int Cristales => cristales;
    public int Energia => energia;
    public int Mineral => mineral;
    public int Esporas => esporas;
    public int Calor => calor;
    public int Acido => acido;
    public int MineralDuro => mineralDuro;
    public int Frio => frio;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool SpendFood(int amount)
    {
        return SpendResource("Comida", amount);
    }

    public bool HasResource(string resourceName, int amount)
    {
        return GetResource(resourceName) >= amount;
    }

    public void AddResource(string resourceName, int amount)
    {
        SetResource(resourceName, GetResource(resourceName) + amount);
    }

    public bool SpendResource(string resourceName, int amount)
    {
        if (!HasResource(resourceName, amount))
        {
            return false;
        }

        SetResource(resourceName, GetResource(resourceName) - amount);
        return true;
    }

    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>
        {
            { "Agua", agua },
            { "Comida", comida },
            { "Biomasa", biomasa },
            { "Cristales", cristales },
            { "Energia", energia },
            { "Mineral", mineral },
            { "Esporas", esporas },
            { "Calor", calor },
            { "Acido", acido },
            { "MineralDuro", mineralDuro },
            { "Frio", frio }
        };
    }

    private int GetResource(string resourceName)
    {
        return resourceName switch
        {
            "Agua" => agua,
            "Comida" => comida,
            "Biomasa" => biomasa,
            "Cristales" => cristales,
            "Energia" => energia,
            "Mineral" => mineral,
            "Esporas" => esporas,
            "Calor" => calor,
            "Acido" => acido,
            "MineralDuro" => mineralDuro,
            "Frio" => frio,
            _ => 0
        };
    }

    private void SetResource(string resourceName, int value)
    {
        value = Mathf.Max(0, value);

        switch (resourceName)
        {
            case "Agua":
                agua = value;
                break;
            case "Comida":
                comida = value;
                break;
            case "Biomasa":
                biomasa = value;
                break;
            case "Cristales":
                cristales = value;
                break;
            case "Energia":
                energia = value;
                break;
            case "Mineral":
                mineral = value;
                break;
            case "Esporas":
                esporas = value;
                break;
            case "Calor":
                calor = value;
                break;
            case "Acido":
                acido = value;
                break;
            case "MineralDuro":
                mineralDuro = value;
                break;
            case "Frio":
                frio = value;
                break;
        }

        ResourcesChanged?.Invoke();
    }
}
