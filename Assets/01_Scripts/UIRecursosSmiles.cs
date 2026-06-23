using TMPro;
using UnityEngine;

public class UIRecursosSmiles : MonoBehaviour
{
    public TextMeshProUGUI textoGelVerde;
    public TextMeshProUGUI textoGotasAzul;
    public TextMeshProUGUI textoEnergiaSolar;
    public TextMeshProUGUI textoNectarMorado;
    public TextMeshProUGUI textoManzanas;
    public TextMeshProUGUI textoAgua;
    public TextMeshProUGUI textoBufandas;

    private void Awake()
    {
        AsegurarTextos();
    }

    public void ActualizarUI(int gel, int gotas, int energia, int nectar, int manzanas = 0, int agua = 0, int bufandas = 0)
    {
        AsegurarTextos();
        ActualizarTexto(textoGelVerde, "x" + gel);
        ActualizarTexto(textoGotasAzul, "x" + gotas);
        ActualizarTexto(textoEnergiaSolar, "x" + energia);
        ActualizarTexto(textoNectarMorado, "x" + nectar);
        ActualizarTexto(textoManzanas, "x" + manzanas);
        ActualizarTexto(textoAgua, "x" + agua);
        ActualizarTexto(textoBufandas, "x" + bufandas);
    }

    private void AsegurarTextos()
    {
        textoGelVerde ??= BuscarTexto("Texto_GelVerde");
        textoGotasAzul ??= BuscarTexto("Texto_GotasAzul");
        textoEnergiaSolar ??= BuscarTexto("Texto_RecursoEnergiaSolar");
        textoNectarMorado ??= BuscarTexto("Texto_RecursoNectarMorado");
        textoManzanas ??= BuscarTexto("Texto_RecursoManzanas");
        textoAgua ??= BuscarTexto("Texto_RecursoAgua");
        textoBufandas ??= BuscarTexto("Texto_RecursoBufandas");
    }

    private static TextMeshProUGUI BuscarTexto(params string[] nombres)
    {
        TextMeshProUGUI[] textos = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (string nombre in nombres)
        {
            foreach (TextMeshProUGUI texto in textos)
            {
                if (texto != null && texto.name == nombre && texto.gameObject.scene.IsValid())
                {
                    return texto;
                }
            }
        }

        return null;
    }

    private static void ActualizarTexto(TextMeshProUGUI texto, string valor)
    {
        if (texto != null)
        {
            texto.text = valor;
        }
    }
}
