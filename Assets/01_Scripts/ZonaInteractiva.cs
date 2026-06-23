using UnityEngine;

public class ZonaInteractiva : MonoBehaviour
{
    [Header("Datos de la zona")]
    public string nombreZona;

    [TextArea]
    public string mensajeInteraccion;

    [Header("Tipo de zona")]
    public TipoZona tipoZona;

    [Header("Estado")]
    public bool jugadorDentro = false;

    private UIInteraccion uiInteraccion;

    private void Start()
    {
        uiInteraccion = FindFirstObjectByType<UIInteraccion>();

        if (uiInteraccion == null)
        {
            Debug.LogWarning("No se encontró UIInteraccion en la escena. Crea un UI_Manager con el script UIInteraccion.");
        }

        if (string.IsNullOrEmpty(mensajeInteraccion))
        {
            mensajeInteraccion = "Presiona E para interactuar con " + nombreZona;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = true;

            if (uiInteraccion != null)
            {
                uiInteraccion.MostrarMensaje(mensajeInteraccion);
            }

            Debug.Log("Entraste a: " + nombreZona);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = false;

            if (uiInteraccion != null)
            {
                uiInteraccion.OcultarMensaje();
            }

            Debug.Log("Saliste de: " + nombreZona);
        }
    }

    private void Update()
    {
        if (jugadorDentro && Input.GetKeyDown(KeyCode.T))
        {
            Interactuar();
        }
    }

    private void Interactuar()
    {
        switch (tipoZona)
        {
            case TipoZona.Casa:
                Debug.Log("CASA activada: aquí luego podrás guardar, dormir o abrir mejoras.");
                break;

            case TipoZona.CultivosPlantas:
                Debug.Log("CULTIVOS DE PLANTAS activado: aquí luego podrás plantar, regar y cosechar.");
                break;

            case TipoZona.GranjaSmiles:
                Debug.Log("GRANJA DE SMILES activada: aquí luego podrás alimentar y cuidar smiles.");
                break;

            case TipoZona.SmilesContaminados:
                Debug.Log("ZONA DE SMILES CONTAMINADOS activada: aquí luego capturarás smiles contaminados.");
                break;

            case TipoZona.RioContaminado:
                Debug.Log("RÍO CONTAMINADO activado: aquí luego recogerás agua contaminada.");
                break;

            case TipoZona.SmilesHongos:
                Debug.Log("ZONA DE SMILES DE HONGOS activada: aquí luego capturarás smiles de hongos.");
                break;

            case TipoZona.SmilesFuego:
                Debug.Log("ZONA DE SMILES DE FUEGO activada: aquí luego capturarás smiles de fuego.");
                break;

            case TipoZona.SmilesMorados:
                Debug.Log("ZONA DE SMILES MORADOS activada: aquí luego capturarás smiles morados.");
                break;
        }
    }
}

public enum TipoZona
{
    Casa,
    CultivosPlantas,
    GranjaSmiles,
    SmilesContaminados,
    RioContaminado,
    SmilesHongos,
    SmilesFuego,
    SmilesMorados
}