using TMPro;
using UnityEngine;

public class UIInventarioSemillas : MonoBehaviour
{
    [Header("Textos de semillas")]
    public TMP_Text textoSemillaVerde;
    public TMP_Text textoSemillaAzul;
    public TMP_Text textoSemillaAmarilla;
    public TMP_Text textoSemillaMorada;

    private void Awake()
    {
        AsegurarTextos();
    }

    private void Start()
    {
        AsegurarTextos();
        ActualizarUI();
    }

    public void ActualizarUI()
    {
        AsegurarTextos();

        if (InventarioSemillas.instancia == null)
        {
            return;
        }

        ActualizarTexto(textoSemillaVerde, "Verde x" + InventarioSemillas.instancia.semillasVerdes);
        ActualizarTexto(textoSemillaAzul, "Azul x" + InventarioSemillas.instancia.semillasAzules);
        ActualizarTexto(textoSemillaAmarilla, "Amarilla x" + InventarioSemillas.instancia.semillasAmarillas);
        ActualizarTexto(textoSemillaMorada, "Morada x" + InventarioSemillas.instancia.semillasMoradas);
    }

    private void AsegurarTextos()
    {
        textoSemillaVerde ??= BuscarTexto("Texto_SemillaVerde");
        textoSemillaAzul ??= BuscarTexto("Texto_SemillaAzul");
        textoSemillaAmarilla ??= BuscarTexto("Texto_SemillaAmarilla");
        textoSemillaMorada ??= BuscarTexto("Texto_SemillaMorada");
    }

    private static TMP_Text BuscarTexto(string nombre)
    {
        TMP_Text[] textos = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text texto in textos)
        {
            if (texto != null && texto.name == nombre && texto.gameObject.scene.IsValid())
            {
                return texto;
            }
        }

        return null;
    }

    private static void ActualizarTexto(TMP_Text texto, string valor)
    {
        if (texto != null)
        {
            texto.text = valor;
        }
    }
}
