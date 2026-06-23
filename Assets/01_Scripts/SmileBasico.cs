using UnityEngine;

public class SmileBasico : MonoBehaviour
{
    [Header("Datos del Smile")]
    public string nombreSmile = "Smile Verde";
    public TipoSemilla alimentoFavorito = TipoSemilla.Verde;

    [Header("Estados")]
    public int hambre = 50;
    public int felicidad = 50;

    [Header("Interacción")]
    public bool jugadorDentro;

    [Header("Producción de recursos")]
    public int cantidadRecursoPorAlimentar = 1; // Cuánto produce cada vez que lo alimentas

    private void Update()
    {
        if (jugadorDentro && Input.GetKeyDown(KeyCode.Y))
        {
            Alimentar();
        }
    }

    private void Alimentar()
    {
        // Verifica que exista el Inventario de Cosechas
        if (InventarioCosechas.instancia == null)
        {
            Debug.LogWarning("No existe InventarioCosechas en la escena.");
            return;
        }

        // Intenta consumir la cosecha correspondiente al alimento favorito
        bool pudoAlimentar = InventarioCosechas.instancia.ConsumirCosecha(alimentoFavorito, 1);

        if (pudoAlimentar)
        {
            // Actualiza hambre y felicidad
            hambre = Mathf.Max(0, hambre - 20);
            felicidad = Mathf.Min(100, felicidad + 15);
            SlimeGenomeInstaller.Ensure(gameObject)?.RegistrarAlimentacion(alimentoFavorito, true);

            Debug.Log(nombreSmile + " fue alimentado. Hambre: " + hambre + " Felicidad: " + felicidad);

            // Produce el recurso correspondiente
            ProducirRecurso();
        }
        else
        {
            SlimeGenomeInstaller.Ensure(gameObject)?.RegistrarAlimentacion(alimentoFavorito, false);
            Debug.Log("No tienes alimento para " + nombreSmile);
        }
    }

    private void ProducirRecurso()
    {
        if (InventarioRecursosSmiles.instancia == null)
        {
            Debug.LogWarning("No existe InventarioRecursosSmiles en la escena.");
            return;
        }

        switch (alimentoFavorito)
        {
            case TipoSemilla.Verde:
                InventarioRecursosSmiles.instancia.AgregarGelVerde(cantidadRecursoPorAlimentar);
                Debug.Log(nombreSmile + " produjo Gel Verde x" + cantidadRecursoPorAlimentar);
                break;

            case TipoSemilla.Azul:
                InventarioRecursosSmiles.instancia.gotasSmileAzul += cantidadRecursoPorAlimentar;
                Debug.Log(nombreSmile + " produjo Gotas Azul x" + cantidadRecursoPorAlimentar);
                break;

            case TipoSemilla.Amarilla:
                InventarioRecursosSmiles.instancia.energiaSmileAmarilla += cantidadRecursoPorAlimentar;
                Debug.Log(nombreSmile + " produjo Energía Solar x" + cantidadRecursoPorAlimentar);
                break;

            case TipoSemilla.Morada:
                InventarioRecursosSmiles.instancia.esenciaSmileMorada += cantidadRecursoPorAlimentar;
                Debug.Log(nombreSmile + " produjo Néctar Morado x" + cantidadRecursoPorAlimentar);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = true;
            Debug.Log("Presiona Y para alimentar a " + nombreSmile);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDentro = false;
        }
    }
}