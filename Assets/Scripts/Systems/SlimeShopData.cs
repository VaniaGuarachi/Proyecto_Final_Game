using UnityEngine;

[System.Serializable]
public class SlimeShopItem
{
    public string slimeName;           // Nombre del slime (Slime Rosa, Slime Planta, etc)
    public string slimeType;           // Tipo (Rosa, Planta, Fuego, Toxico)
    public int price;                  // Precio que cuesta (cuántos cristales)
    public Sprite slimeIcon;           // La imagen del slime
    public string description;         // Descripción pequeña
}

public class SlimeShopData : MonoBehaviour
{
    public static SlimeShopData Instance { get; private set; }

    [Header("Slimes disponibles en la tienda")]
    [SerializeField] private SlimeShopItem[] availableSlimes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Si no hay slimes configurados en el Inspector, crear una lista por defecto
        if (availableSlimes == null || availableSlimes.Length == 0)
        {
            Debug.Log("SlimeShopData: No hay slimes configurados, creando slimes por defecto.");
            availableSlimes = new SlimeShopItem[4];

            availableSlimes[0] = new SlimeShopItem
            {
                slimeName = "Slime Rosa",
                slimeType = "Rosa",
                price = 300,
                description = "Un adorable slime rosa que rebota feliz",
                slimeIcon = GeneratePlaceholderSprite(new Color(1f, 0.6f, 0.8f), 128, "Slime Rosa")
            };

            availableSlimes[1] = new SlimeShopItem
            {
                slimeName = "Slime Planta",
                slimeType = "Planta",
                price = 250,
                description = "Un slime verde con hojas, de naturaleza",
                slimeIcon = GeneratePlaceholderSprite(new Color(0.45f, 0.85f, 0.45f), 128, "Slime Planta")
            };

            availableSlimes[2] = new SlimeShopItem
            {
                slimeName = "Slime Fuego",
                slimeType = "Fuego",
                price = 400,
                description = "Un peligroso slime lleno de energía y fuego",
                slimeIcon = GeneratePlaceholderSprite(new Color(1f, 0.45f, 0.2f), 128, "Slime Fuego")
            };

            availableSlimes[3] = new SlimeShopItem
            {
                slimeName = "Slime Toxico",
                slimeType = "Toxico",
                price = 350,
                description = "Un slime venenoso y fascinante",
                slimeIcon = GeneratePlaceholderSprite(new Color(0.6f, 0.2f, 0.8f), 128, "Slime Toxico")
            };
        }

        // Asegurar que cada slime tenga algún sprite (si el inspector dejó alguno vacío)
        for (int i = 0; i < availableSlimes.Length; i++)
        {
            SlimeShopItem item = availableSlimes[i];
            if (item == null)
                continue;

            if (item.slimeIcon == null)
            {
                // Elegir color por tipo
                Color color = Color.gray;
                string type = (item.slimeType ?? "").ToLower();
                if (type.Contains("rosa") || type.Contains("pink")) color = new Color(1f, 0.6f, 0.8f);
                else if (type.Contains("planta") || type.Contains("plant") || type.Contains("verde")) color = new Color(0.45f, 0.85f, 0.45f);
                else if (type.Contains("fuego") || type.Contains("fire")) color = new Color(1f, 0.45f, 0.2f);
                else if (type.Contains("toxico") || type.Contains("toxic") || type.Contains("veneno")) color = new Color(0.6f, 0.2f, 0.8f);
                else color = new Color(0.8f, 0.8f, 0.8f);

                item.slimeIcon = GeneratePlaceholderSprite(color, 128, item.slimeName ?? "Slime");
                Debug.Log($"SlimeShopData: Generado sprite por código para '{item.slimeName}'. Reemplaza con un PNG en Assets/Sprites/ si lo deseas.");
            }
        }
    }

    /// <summary>
    /// Genera un sprite circular simple de un color dado (placeholder runtime).
    /// </summary>
    private Sprite GeneratePlaceholderSprite(Color fillColor, int size = 128, string name = "placeholder")
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        // Dibujar un círculo centrado
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.45f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    tex.SetPixel(x, y, fillColor);
                }
                else if (dist <= radius + 1f)
                {
                    // anti-aliased edge
                    Color c = Color.Lerp(fillColor, clear, (dist - radius));
                    tex.SetPixel(x, y, c);
                }
            }
        }

        tex.Apply();

        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        s.name = name + "_placeholder";
        return s;
    }

    /// <summary>
    /// Obtiene todos los slimes disponibles en la tienda
    /// </summary>
    public SlimeShopItem[] GetAvailableSlimes()
    {
        return availableSlimes;
    }

    /// <summary>
    /// Obtiene un slime específico por índice
    /// </summary>
    public SlimeShopItem GetSlimeByIndex(int index)
    {
        if (index >= 0 && index < availableSlimes.Length)
        {
            return availableSlimes[index];
        }
        return null;
    }

    /// <summary>
    /// Verifica si el jugador puede comprar un slime
    /// </summary>
    public bool CanBuySlime(int slimeIndex)
    {
        if (slimeIndex >= 0 && slimeIndex < availableSlimes.Length)
        {
            SlimeShopItem item = availableSlimes[slimeIndex];
            return ResourceManager.Instance.Cristales >= item.price;
        }
        return false;
    }

    /// <summary>
    /// Compra un slime (resta cristales)
    /// </summary>
    public bool BuySlime(int slimeIndex)
    {
        if (CanBuySlime(slimeIndex))
        {
            SlimeShopItem item = availableSlimes[slimeIndex];
            return ResourceManager.Instance.SpendResource("Cristales", item.price);
        }
        return false;
    }
}
