using UnityEngine;

[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public class AnimacionEscaleraPlayer : MonoBehaviour
{
    [Header("Escalera")]
    public float fotogramasPorSegundo = 8f;
    public float cabezaAtras = 14f;
    public float movimientoManos = 0.18f;
    public float movimientoPies = 0.12f;

    private Transform cuerpo;
    private Transform cara;
    private Transform manoDerecha;
    private Transform manoIzquierda;
    private Transform pieDerecho;
    private Transform pieIzquierdo;

    private EstadoTransform cuerpoBase;
    private EstadoTransform caraBase;
    private EstadoTransform manoDerechaBase;
    private EstadoTransform manoIzquierdaBase;
    private EstadoTransform pieDerechoBase;
    private EstadoTransform pieIzquierdoBase;

    private bool escalando;
    private float direccion = 1f;
    private float intensidadMovimiento;
    private float tiempo;

    private void Awake()
    {
        cuerpo = Buscar("Player1_Cuerpo");
        cara = Buscar("Player1_Cara");
        manoDerecha = Buscar("Player1_ManoDerecha");
        manoIzquierda = Buscar("Player1_ManoIzquierda");
        pieDerecho = Buscar("Player1_PieDerecho");
        pieIzquierdo = Buscar("Player1_PieIzquierdo");

        cuerpoBase = new EstadoTransform(cuerpo);
        caraBase = new EstadoTransform(cara);
        manoDerechaBase = new EstadoTransform(manoDerecha);
        manoIzquierdaBase = new EstadoTransform(manoIzquierda);
        pieDerechoBase = new EstadoTransform(pieDerecho);
        pieIzquierdoBase = new EstadoTransform(pieIzquierdo);
    }

    public void ActualizarEscalera(bool activo, float entradaVertical)
    {
        escalando = activo;
        intensidadMovimiento = Mathf.Abs(entradaVertical);

        if (Mathf.Abs(entradaVertical) > 0.05f)
        {
            direccion = Mathf.Sign(entradaVertical);
        }
    }

    private void LateUpdate()
    {
        if (!escalando || cuerpo == null)
        {
            return;
        }

        tiempo += Time.deltaTime * fotogramasPorSegundo * direccion * intensidadMovimiento;
        int fotograma = Mod(Mathf.FloorToInt(tiempo), 4);
        AplicarFotograma(fotograma);
    }

    private void AplicarFotograma(int fotograma)
    {
        float lado = fotograma < 2 ? 1f : -1f;
        float empuje = fotograma == 1 || fotograma == 3 ? 1f : 0f;

        Aplicar(cuerpo, cuerpoBase, new Vector3(lado * 0.025f, empuje * 0.035f, 0f), lado * -2f);
        Aplicar(cara, caraBase, new Vector3(-0.07f, 0.07f, 0f), -cabezaAtras);

        Aplicar(manoDerecha, manoDerechaBase, new Vector3(0.03f, lado * movimientoManos, 0f), lado * -26f);
        Aplicar(manoIzquierda, manoIzquierdaBase, new Vector3(-0.03f, -lado * movimientoManos, 0f), lado * 26f);

        Aplicar(pieDerecho, pieDerechoBase, new Vector3(0.02f, -lado * movimientoPies, 0f), lado * 18f);
        Aplicar(pieIzquierdo, pieIzquierdoBase, new Vector3(-0.02f, lado * movimientoPies, 0f), lado * -18f);
    }

    private void Aplicar(Transform objetivo, EstadoTransform baseTransform, Vector3 offset, float rotacionZ)
    {
        if (objetivo == null)
        {
            return;
        }

        objetivo.localPosition = baseTransform.posicion + offset;
        objetivo.localRotation = Quaternion.Euler(0f, 0f, baseTransform.rotacionZ + rotacionZ);
    }

    private Transform Buscar(string nombre)
    {
        Transform[] hijos = GetComponentsInChildren<Transform>(true);
        foreach (Transform hijo in hijos)
        {
            if (hijo.name == nombre)
            {
                return hijo;
            }
        }

        return null;
    }

    private int Mod(int valor, int modulo)
    {
        return (valor % modulo + modulo) % modulo;
    }

    private struct EstadoTransform
    {
        public Vector3 posicion;
        public float rotacionZ;

        public EstadoTransform(Transform transform)
        {
            if (transform == null)
            {
                posicion = Vector3.zero;
                rotacionZ = 0f;
                return;
            }

            posicion = transform.localPosition;
            rotacionZ = transform.localEulerAngles.z;

            if (rotacionZ > 180f)
            {
                rotacionZ -= 360f;
            }
        }
    }
}
