using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("Panel Principal")]
    [SerializeField] private GameObject shopPanel;           // El panel que contiene todo

    [Header("Botones de Slimes")]
    [SerializeField] private Transform slimeButtonsContainer; // Donde van los botones
    [SerializeField] private GameObject slimeButtonPrefab;    // Prefab del botón de cada slime

    [Header("Información del Slime")]
    [SerializeField] private Image slimeDisplayImage;        // Imagen del slime seleccionado
    [SerializeField] private TextMeshProUGUI slimeNameText;  // Nombre del slime
    [SerializeField] private TextMeshProUGUI priceText;       // Precio
    [SerializeField] private TextMeshProUGUI descriptionText; // Descripción
    [SerializeField] private Button buyButton;               // Botón de comprar
    [SerializeField] private TextMeshProUGUI crystalsText;    // Muestra cuántos cristales tienes

    [Header("Botón Cerrar")]
    [SerializeField] private Button closeButton;             // Botón para cerrar la tienda

    private SlimeShopItem currentSelectedSlime;
    private int currentSelectedIndex = -1;
    private List<Button> slimeButtons = new List<Button>();

    private void Start()
    {
        // Inicialmente la tienda está cerrada
        shopPanel.SetActive(false);

        // Crear los botones de los slimes
        CreateSlimeButtons();

        // Configurar botones
        buyButton.onClick.AddListener(OnBuyButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        // Actualizar cristales cada vez que cambian
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourcesChanged += UpdateCrystalsDisplay;
        }
    }

    /// <summary>
    /// Crea un botón para cada slime disponible
    /// </summary>
    private void CreateSlimeButtons()
    {
        SlimeShopItem[] availableSlimes = SlimeShopData.Instance.GetAvailableSlimes();

        for (int i = 0; i < availableSlimes.Length; i++)
        {
            SlimeShopItem slime = availableSlimes[i];

            GameObject buttonObj = null;
            // Instanciar el prefab del botón si existe
            if (slimeButtonPrefab != null)
            {
                buttonObj = Instantiate(slimeButtonPrefab, slimeButtonsContainer);
            }
            else
            {
                // Fallback: crear un botón simple por código para evitar errores si no hay prefab
                buttonObj = new GameObject($"SlimeButton_{i}", typeof(RectTransform));
                buttonObj.transform.SetParent(slimeButtonsContainer, false);

                // Agregar Image y Button
                Image img = buttonObj.AddComponent<Image>();
                img.color = Color.white;
                Button btn = buttonObj.AddComponent<Button>();

                // Crear texto hijo
                GameObject txtObj = new GameObject("Label", typeof(RectTransform));
                txtObj.transform.SetParent(buttonObj.transform, false);
                var txt = txtObj.AddComponent<TextMeshProUGUI>();
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 24;
                RectTransform rt = buttonObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100, 100);
                RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
                rtTxt.anchorMin = new Vector2(0, 0);
                rtTxt.anchorMax = new Vector2(1, 1);
                rtTxt.offsetMin = Vector2.zero;
                rtTxt.offsetMax = Vector2.zero;

                // Añadir una imagen hija para el icono
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(buttonObj.transform, false);
                var iconImg = iconObj.AddComponent<Image>();
                RectTransform rtIcon = iconObj.GetComponent<RectTransform>();
                rtIcon.anchorMin = new Vector2(0.1f, 0.2f);
                rtIcon.anchorMax = new Vector2(0.9f, 0.9f);
                rtIcon.offsetMin = Vector2.zero;
                rtIcon.offsetMax = Vector2.zero;
            }

            // Obtener el botón
            Button button = buttonObj.GetComponent<Button>();
            slimeButtons.Add(button);

            // Obtener la imagen del botón y asignarla
            Image buttonImage = null;
            // Si el prefab tenía una Image en hijos, preferir esa
            buttonImage = buttonObj.GetComponentInChildren<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = slime.slimeIcon;
            }

            // Obtener el texto (nombre) del botón
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = slime.slimeName;
            }

            // Agregar listener al botón para que muestre la información del slime
            int slimeIndex = i;
            button.onClick.AddListener(() => OnSlimeButtonClicked(slimeIndex));
        }

        // Seleccionar el primer slime por defecto
        if (slimeButtons.Count > 0)
        {
            OnSlimeButtonClicked(0);
        }
    }

    /// <summary>
    /// Se llama cuando se presiona un botón de slime
    /// </summary>
    private void OnSlimeButtonClicked(int slimeIndex)
    {
        currentSelectedIndex = slimeIndex;
        currentSelectedSlime = SlimeShopData.Instance.GetSlimeByIndex(slimeIndex);

        if (currentSelectedSlime == null)
            return;

        // Actualizar la información mostrada
        slimeDisplayImage.sprite = currentSelectedSlime.slimeIcon;
        slimeNameText.text = currentSelectedSlime.slimeName;
        priceText.text = $"Precio: {currentSelectedSlime.price} Cristales";
        descriptionText.text = currentSelectedSlime.description;

        // Cambiar el color del botón para indicar que está seleccionado
        UpdateButtonSelection();

        // Actualizar si se puede comprar
        UpdateBuyButton();
    }

    /// <summary>
    /// Actualiza el color del botón seleccionado
    /// </summary>
    private void UpdateButtonSelection()
    {
        for (int i = 0; i < slimeButtons.Count; i++)
        {
            ColorBlock colors = slimeButtons[i].colors;

            if (i == currentSelectedIndex)
            {
                // Botón seleccionado - color más brillante
                colors.normalColor = Color.yellow;
            }
            else
            {
                // Botones no seleccionados - color normal
                colors.normalColor = Color.white;
            }

            slimeButtons[i].colors = colors;
        }
    }

    /// <summary>
    /// Se llama cuando se presiona el botón "COMPRAR"
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (currentSelectedIndex < 0)
            return;

        if (SlimeShopData.Instance.BuySlime(currentSelectedIndex))
        {
            Debug.Log($"¡Compraste {currentSelectedSlime.slimeName}!");
            // Aquí podrías agregar sonido de compra, animación, etc.
        }
        else
        {
            Debug.Log("¡No tienes suficientes cristales!");
        }

        UpdateBuyButton();
    }

    /// <summary>
    /// Actualiza el estado del botón de compra (habilitado o deshabilitado)
    /// </summary>
    private void UpdateBuyButton()
    {
        if (currentSelectedSlime == null)
        {
            buyButton.interactable = false;
            return;
        }

        bool canBuy = ResourceManager.Instance.Cristales >= currentSelectedSlime.price;
        buyButton.interactable = canBuy;

        // Cambiar color si no se puede comprar
        ColorBlock colors = buyButton.colors;
        if (canBuy)
        {
            colors.normalColor = Color.green;
        }
        else
        {
            colors.normalColor = Color.red;
        }
        buyButton.colors = colors;
    }

    /// <summary>
    /// Actualiza la pantalla de cristales
    /// </summary>
    private void UpdateCrystalsDisplay()
    {
        crystalsText.text = $"Cristales: {ResourceManager.Instance.Cristales}";
        UpdateBuyButton();
    }

    /// <summary>
    /// Se llama cuando se presiona el botón "CERRAR"
    /// </summary>
    private void OnCloseButtonClicked()
    {
        CloseShop();
    }

    /// <summary>
    /// Abre la tienda
    /// </summary>
    public void OpenShop()
    {
        shopPanel.SetActive(true);
        UpdateCrystalsDisplay();
        GameManager.Instance.PauseGame(); // Pausar el juego mientras está la tienda abierta
    }

    /// <summary>
    /// Cierra la tienda
    /// </summary>
    public void CloseShop()
    {
        shopPanel.SetActive(false);
        GameManager.Instance.ResumeGame(); // Reanudar el juego
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourcesChanged -= UpdateCrystalsDisplay;
        }
    }
}
