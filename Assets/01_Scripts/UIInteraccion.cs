using TMPro;
using UnityEngine;

public class UIInteraccion : MonoBehaviour
{
    [Header("Texto de interacción")]
    public TMP_Text textoInteraccion;

    private void Start()
    {
        OcultarMensaje();
    }

    public void MostrarMensaje(string mensaje)
    {
        if (textoInteraccion == null)
        {
            Debug.LogWarning("No asignaste el texto de interacción en UIInteraccion.");
            return;
        }

        textoInteraccion.text = mensaje;
        textoInteraccion.gameObject.SetActive(true);
    }

    public void OcultarMensaje()
    {
        if (textoInteraccion == null)
        {
            return;
        }

        textoInteraccion.gameObject.SetActive(false);
    }
}