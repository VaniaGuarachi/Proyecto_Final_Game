using UnityEngine;

public class SemillaRecolectable : MonoBehaviour
{
    [Header("Datos de la semilla")]
    public TipoSemilla tipoSemilla;
    public int cantidad = 1;

    [Header("Reaparicion")]
    public bool reaparecerDespuesDeRecolectar = true;
    public float tiempoReaparicion = 20f;

    [Header("Movimiento visual")]
    public bool flotar = true;
    public float velocidadFlotacion = 2f;
    public float alturaFlotacion = 0.08f;

    private Vector3 posicionInicial;
    private Renderer[] renderersSemilla;
    private Collider2D[] collidersSemilla;
    private bool recolectada;

    private void Start()
    {
        posicionInicial = transform.position;
        renderersSemilla = GetComponentsInChildren<Renderer>(true);
        collidersSemilla = GetComponentsInChildren<Collider2D>(true);
    }

    private void Update()
    {
        if (flotar && !recolectada)
        {
            float movimientoY = Mathf.Sin(Time.time * velocidadFlotacion) * alturaFlotacion;
            transform.position = posicionInicial + new Vector3(0, movimientoY, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (recolectada || !other.CompareTag("Player"))
        {
            return;
        }

        if (InventarioSemillas.instancia != null)
        {
            InventarioSemillas.instancia.AgregarSemilla(tipoSemilla, cantidad);
            Recolectar();
        }
        else
        {
            Debug.LogWarning("No existe InventarioSemillas en la escena. Crea un GameManager con el script InventarioSemillas.");
        }
    }

    private void Recolectar()
    {
        if (!reaparecerDespuesDeRecolectar)
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(ReaparecerDespuesDeTiempo());
    }

    private System.Collections.IEnumerator ReaparecerDespuesDeTiempo()
    {
        recolectada = true;
        CambiarEstadoVisual(false);
        transform.position = posicionInicial;

        yield return new WaitForSeconds(tiempoReaparicion);

        recolectada = false;
        transform.position = posicionInicial;
        CambiarEstadoVisual(true);
    }

    private void CambiarEstadoVisual(bool visible)
    {
        foreach (Renderer rendererSemilla in renderersSemilla)
        {
            rendererSemilla.enabled = visible;
        }

        foreach (Collider2D colliderSemilla in collidersSemilla)
        {
            colliderSemilla.enabled = visible;
        }
    }
}
