using UnityEngine;

[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public class AnimacionEscaleraPlayer : MonoBehaviour
{
    [Header("Sprite de escalera")]
    public Sprite spriteEspalda;
    public Vector2 offsetSprite = new Vector2(0f, 0.02f);
    public float escalaSprite = 0.72f;
    public int ordenDibujo = 1000;

    [Header("Animacion")]
    public float fotogramasPorSegundo = 9f;
    public float inclinacionCuerpo = 3f;
    public float alturaPaso = 0.12f;

    private Animator animatorCuerpo;
    private SpriteRenderer[] renderersFrontales;
    private bool[] estadosRenderersFrontales;
    private Transform spriteEscalera;
    private SpriteRenderer rendererEscalera;
    private bool escalando;
    private float direccion = 1f;
    private float intensidadMovimiento;
    private float tiempo;

    private static readonly float[] Balanceo = { -1f, -0.55f, 0.15f, 1f, 0.45f, -0.2f };
    private static readonly float[] Elevacion = { -0.55f, 0.05f, 0.55f, 0.8f, 0.35f, -0.25f };
    private static readonly float[] Compresion = { 0.98f, 1.01f, 1.03f, 1f, 1.02f, 0.99f };

    private void Awake()
    {
        animatorCuerpo = GetComponentInChildren<Animator>(true);
        renderersFrontales = animatorCuerpo != null
            ? animatorCuerpo.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
        estadosRenderersFrontales = new bool[renderersFrontales.Length];
        CrearSpriteEscalera();
    }

    public void ActualizarEscalera(bool activo, float entradaVertical)
    {
        Player2VisualAnimator player2 = GetComponent<Player2VisualAnimator>();
        if (player2 != null && player2.IsUsingPlayer2)
        {
            if (escalando)
            {
                MostrarPoseEscalera(false);
                escalando = false;
            }

            player2.SetClimbing(activo, entradaVertical);
            return;
        }

        if (!activo)
        {
            if (escalando)
            {
                MostrarPoseEscalera(false);
            }

            escalando = false;
            intensidadMovimiento = 0f;
            return;
        }

        if (!escalando)
        {
            tiempo = entradaVertical < 0f ? 5.99f : 0f;
            MostrarPoseEscalera(true);
        }

        escalando = true;
        intensidadMovimiento = Mathf.Abs(entradaVertical);

        if (intensidadMovimiento > 0.05f)
        {
            direccion = Mathf.Sign(entradaVertical);
        }
    }

    private void LateUpdate()
    {
        if (!escalando || spriteEscalera == null)
        {
            return;
        }

        tiempo += Time.deltaTime * fotogramasPorSegundo * direccion * intensidadMovimiento;
        AplicarFotograma(Mod(Mathf.FloorToInt(tiempo), 6));
    }

    private void CrearSpriteEscalera()
    {
        GameObject visual = new GameObject("Player1_Escalera_Espalda");
        visual.transform.SetParent(transform, false);
        spriteEscalera = visual.transform;

        rendererEscalera = visual.AddComponent<SpriteRenderer>();
        rendererEscalera.sprite = spriteEspalda;
        rendererEscalera.color = Color.white;
        rendererEscalera.sortingOrder = ordenDibujo;
        rendererEscalera.enabled = false;

        spriteEscalera.localPosition = new Vector3(offsetSprite.x, offsetSprite.y, -1f);
        spriteEscalera.localScale = Vector3.one * escalaSprite;
    }

    private void MostrarPoseEscalera(bool mostrar)
    {
        for (int i = 0; i < renderersFrontales.Length; i++)
        {
            SpriteRenderer rendererFrontal = renderersFrontales[i];

            if (rendererFrontal == null)
            {
                continue;
            }

            if (mostrar)
            {
                estadosRenderersFrontales[i] = rendererFrontal.enabled;
                rendererFrontal.enabled = false;
            }
            else
            {
                rendererFrontal.enabled = estadosRenderersFrontales[i];
            }
        }

        if (rendererEscalera != null)
        {
            rendererEscalera.enabled = mostrar && spriteEspalda != null;
        }

        if (!mostrar && spriteEscalera != null)
        {
            spriteEscalera.localPosition = new Vector3(offsetSprite.x, offsetSprite.y, -1f);
            spriteEscalera.localRotation = Quaternion.identity;
            spriteEscalera.localScale = Vector3.one * escalaSprite;
        }
    }

    private void AplicarFotograma(int fotograma)
    {
        float lado = Balanceo[fotograma];
        float escalaY = Compresion[fotograma];

        spriteEscalera.localPosition = new Vector3(
            offsetSprite.x + lado * 0.035f,
            offsetSprite.y + Elevacion[fotograma] * alturaPaso,
            -1f);
        spriteEscalera.localRotation = Quaternion.Euler(0f, 0f, lado * inclinacionCuerpo);
        spriteEscalera.localScale = new Vector3(
            escalaSprite * (2f - escalaY),
            escalaSprite * escalaY,
            escalaSprite);
    }

    private static int Mod(int valor, int modulo)
    {
        return (valor % modulo + modulo) % modulo;
    }

    private void OnDisable()
    {
        if (escalando)
        {
            MostrarPoseEscalera(false);
            escalando = false;
        }
    }
}
