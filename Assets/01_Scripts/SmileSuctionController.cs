using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmileSuctionController : MonoBehaviour
{
    [Header("Controles")]
    public KeyCode teclaSuccionar = KeyCode.R;
    public KeyCode teclaSoltar = KeyCode.Y;
    public KeyCode teclaAlimentar = KeyCode.Y;

    [Header("Succionador")]
    public Transform bocaSuccionador;
    public float radioSuccion = 2.8f;
    public float duracionSuccion = 0.75f;
    public int particulasSuccion = 16;

    [Header("Smiles guardados")]
    public List<SmileBasico> smilesGuardados = new List<SmileBasico>();
    public List<GameObject> unidadesGuardadas = new List<GameObject>();

    private bool succionando;
    private LineRenderer rayoSuccion;
    private Sprite particulaSprite;
    private Sprite panelSprite;
    private Sprite slotSprite;
    private GameObject panelUI;
    private Image iconoSmileUI;
    private TMP_Text nombreSmileUI;
    private TMP_Text estadoSmileUI;
    
    private AudioSource suctionAudio;

    private static readonly Color PanelFill = new Color(0.055f, 0.075f, 0.105f, 0.90f);
    private static readonly Color PanelBorder = new Color(0.96f, 0.78f, 0.36f, 0.92f);
    private static readonly Color SlotFill = new Color(0.11f, 0.145f, 0.17f, 0.84f);
    private static readonly Color SlotBorder = new Color(1f, 1f, 1f, 0.18f);

    public int CantidadSmilesGuardados => unidadesGuardadas.Count(unidad => unidad != null);

    private void Awake()
    {
        if (bocaSuccionador == null)
        {
            bocaSuccionador = BuscarHijoPorNombre(transform, "Subcionador") ?? transform;
        }

        CrearSpritesUI();
        CrearRayoSuccion();
        
        suctionAudio = gameObject.AddComponent<AudioSource>();
        suctionAudio.clip = Resources.Load<AudioClip>("Sonidos/succionDeSlime");
        suctionAudio.playOnAwake = false;
    }

    private void Start()
    {
        CrearPanelUI();
        ActualizarPanelUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaSuccionar))
        {
            IntentarSuccionar();
        }

        if (Input.GetKeyDown(teclaSoltar))
        {
            if (IntentarAlimentarSmileCercano())
            {
                return;
            }

            if (CantidadSmilesGuardados > 0)
            {
                SoltarSmile();
            }
        }
    }

    private void IntentarSuccionar()
    {
        if (succionando)
        {
            return;
        }

        SmileBasico smileCercano = BuscarSmileCercano();
        if (smileCercano == null)
        {
            GameObject slimeEspecial = BuscarSlimeEspecialCercano();
            if (slimeEspecial != null)
            {
                StartCoroutine(SuccionarSlimeEspecial(slimeEspecial));
                return;
            }

            Debug.Log("No hay ningun smile cerca para succionar.");
            return;
        }

        StartCoroutine(SuccionarSmile(smileCercano));
    }

    private SmileBasico BuscarSmileCercano()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(bocaSuccionador.position, radioSuccion);
        SmileBasico mejorSmile = null;
        float mejorDistancia = float.MaxValue;

        foreach (Collider2D collider in colliders)
        {
            SmileBasico smile = collider.GetComponentInParent<SmileBasico>();
            if (smile == null || !smile.gameObject.activeInHierarchy || unidadesGuardadas.Contains(smile.gameObject))
            {
                continue;
            }

            float distancia = Vector2.Distance(bocaSuccionador.position, smile.transform.position);
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorSmile = smile;
            }
        }

        return mejorSmile;
    }

    private GameObject BuscarSlimeEspecialCercano()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(bocaSuccionador.position, radioSuccion);
        GameObject mejorSlime = null;
        float mejorDistancia = float.MaxValue;

        foreach (Collider2D collider in colliders)
        {
            GameObject slime = ObtenerSlimeEspecialDesdeCollider(collider);
            if (slime == null || !slime.activeInHierarchy || unidadesGuardadas.Contains(slime))
            {
                continue;
            }

            float distancia = Vector2.Distance(bocaSuccionador.position, slime.transform.position);
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorSlime = slime;
            }
        }

        return mejorSlime;
    }

    private static GameObject ObtenerSlimeEspecialDesdeCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return null;
        }

        CleanerSlimeAI cleaner = collider.GetComponentInParent<CleanerSlimeAI>();
        if (cleaner != null)
        {
            return cleaner.gameObject;
        }

        ColdSlimeAI cold = collider.GetComponentInParent<ColdSlimeAI>();
        if (cold != null)
        {
            return cold.gameObject;
        }

        FireSlimeAI fire = collider.GetComponentInParent<FireSlimeAI>();
        if (fire != null)
        {
            return fire.gameObject;
        }

        return null;
    }

    private bool IntentarAlimentarSmileCercano()
    {
        SmileBasico smileCercano = BuscarSmileCercano();
        if (smileCercano == null || unidadesGuardadas.Contains(smileCercano.gameObject))
        {
            return false;
        }

        if (smileCercano.jugadorDentro)
        {
            return true;
        }

        smileCercano.SendMessage("Alimentar", SendMessageOptions.DontRequireReceiver);
        return true;
    }

    private IEnumerator SuccionarSmile(SmileBasico smile)
    {
        succionando = true;
        Vector3 inicio = smile.transform.position;
        Vector3 escalaInicial = smile.transform.localScale;
        Quaternion rotacionInicial = smile.transform.rotation;
        Collider2D[] colliders = smile.GetComponentsInChildren<Collider2D>(true);
        Rigidbody2D rbSmile = smile.GetComponent<Rigidbody2D>();
        SmileVerdeEvolucion evolucion = smile.GetComponent<SmileVerdeEvolucion>();
        SmileAmarilloEvolucion evolucionAmarilla = smile.GetComponent<SmileAmarilloEvolucion>();
        SpriteRenderer rendererSmile = smile.GetComponentInChildren<SpriteRenderer>(true);
        Sprite icono = rendererSmile != null ? rendererSmile.sprite : null;

        if (evolucion != null)
        {
            evolucion.enabled = false;
        }

        if (evolucionAmarilla != null)
        {
            evolucionAmarilla.enabled = false;
        }

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        if (rbSmile != null)
        {
            rbSmile.linearVelocity = Vector2.zero;
            rbSmile.simulated = false;
        }

        rayoSuccion.enabled = true;
        SpawnParticulasSuccion(smile.transform);
        
        if (suctionAudio != null)
        {
            suctionAudio.Play();
        }

        float tiempo = 0f;
        while (tiempo < duracionSuccion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionSuccion);
            float curva = Mathf.SmoothStep(0f, 1f, t);
            Vector3 destino = bocaSuccionador.position;

            smile.transform.position = Vector3.Lerp(inicio, destino, curva);
            smile.transform.localScale = Vector3.Lerp(escalaInicial, escalaInicial * 0.18f, curva);
            smile.transform.rotation = rotacionInicial * Quaternion.Euler(0f, 0f, curva * 540f);

            rayoSuccion.SetPosition(0, destino);
            rayoSuccion.SetPosition(1, smile.transform.position);

            yield return null;
        }

        rayoSuccion.enabled = false;
        smile.transform.position = bocaSuccionador.position;
        smile.transform.localScale = escalaInicial;
        smile.transform.rotation = rotacionInicial;
        smile.gameObject.SetActive(false);

        if (!smilesGuardados.Contains(smile))
        {
            smilesGuardados.Add(smile);
        }

        if (!unidadesGuardadas.Contains(smile.gameObject))
        {
            unidadesGuardadas.Add(smile.gameObject);
        }

        ActualizarPanelUI(icono);
        succionando = false;
        Debug.Log("Succionaste a " + smile.nombreSmile + ". Total guardados: " + CantidadSmilesGuardados);
    }

    private IEnumerator SuccionarSlimeEspecial(GameObject slime)
    {
        succionando = true;
        Vector3 inicio = slime.transform.position;
        Vector3 escalaInicial = slime.transform.localScale;
        Quaternion rotacionInicial = slime.transform.rotation;
        Collider2D[] colliders = slime.GetComponentsInChildren<Collider2D>(true);
        Rigidbody2D rbSlime = slime.GetComponent<Rigidbody2D>();
        Sprite icono = ObtenerSpriteUnidad(slime);
        string nombreSlime = ObtenerNombreUnidad(slime);

        SetSlimeAiEnabled(slime, false);

        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        if (rbSlime != null)
        {
            rbSlime.linearVelocity = Vector2.zero;
            rbSlime.simulated = false;
        }

        rayoSuccion.enabled = true;
        SpawnParticulasSuccion(slime.transform);

        if (suctionAudio != null)
        {
            suctionAudio.Play();
        }

        float tiempo = 0f;
        while (tiempo < duracionSuccion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionSuccion);
            float curva = Mathf.SmoothStep(0f, 1f, t);
            Vector3 destino = bocaSuccionador.position;

            slime.transform.position = Vector3.Lerp(inicio, destino, curva);
            slime.transform.localScale = Vector3.Lerp(escalaInicial, escalaInicial * 0.18f, curva);
            slime.transform.rotation = rotacionInicial * Quaternion.Euler(0f, 0f, curva * 540f);

            rayoSuccion.SetPosition(0, destino);
            rayoSuccion.SetPosition(1, slime.transform.position);

            yield return null;
        }

        rayoSuccion.enabled = false;
        slime.transform.localScale = escalaInicial;
        slime.transform.rotation = rotacionInicial;
        slime.gameObject.SetActive(false);

        if (!unidadesGuardadas.Contains(slime.gameObject))
        {
            unidadesGuardadas.Add(slime.gameObject);
        }

        SlimeAutoSpawner.ReponerAhora();
        ActualizarPanelUI(icono);
        succionando = false;
        Debug.Log("Succionaste a " + nombreSlime + ". Se repondran slimes si quedan pocos.");
    }

    private void SoltarSmile()
    {
        if (CantidadSmilesGuardados == 0 || succionando)
        {
            return;
        }

        Vector3 direccion = transform.rotation.eulerAngles.y > 90f ? Vector3.left : Vector3.right;
        Vector3 posicionSalida = bocaSuccionador.position + direccion * 1.2f + Vector3.up * 0.25f;
        ColocarUltimoSmileEn(posicionSalida);
    }

    public bool ColocarUltimoSmileEn(Vector3 posicionSalida)
    {
        GameObject unidadParaSoltar = ExtraerUltimaUnidadGuardada();
        if (unidadParaSoltar == null)
        {
            return false;
        }

        ActivarUnidadEnPosicion(unidadParaSoltar, posicionSalida);
        Debug.Log("Soltaste a " + ObtenerNombreUnidad(unidadParaSoltar) + ". Total guardados: " + CantidadSmilesGuardados);
        ActualizarPanelUI();
        return true;
    }

    public bool ColocarUltimoSmileEnGranja(GranjaColocada granja)
    {
        if (granja == null)
        {
            return false;
        }

        GameObject unidadParaSoltar = ExtraerUltimaUnidadGuardada();
        if (unidadParaSoltar == null)
        {
            return false;
        }

        ActivarUnidadEnPosicion(unidadParaSoltar, granja.ObtenerPosicionInteriorAleatoria());
        SmileEnGranja.Asignar(unidadParaSoltar, granja);
        Debug.Log("Colocaste a " + ObtenerNombreUnidad(unidadParaSoltar) + " dentro de la granja.");
        ActualizarPanelUI();
        return true;
    }

    private SmileBasico ExtraerUltimoSmileGuardado()
    {
        if (CantidadSmilesGuardados == 0 || succionando)
        {
            return null;
        }

        LimpiarSmilesGuardadosNulos();
        SmileBasico smileParaSoltar = smilesGuardados[smilesGuardados.Count - 1];
        smilesGuardados.RemoveAt(smilesGuardados.Count - 1);
        
        if (suctionAudio != null)
        {
            suctionAudio.Play();
        }
        
        return smileParaSoltar;
    }

    private GameObject ExtraerUltimaUnidadGuardada()
    {
        if (CantidadSmilesGuardados == 0 || succionando)
        {
            return null;
        }

        LimpiarUnidadesGuardadasNulas();
        if (unidadesGuardadas.Count == 0)
        {
            return null;
        }

        GameObject unidad = unidadesGuardadas[unidadesGuardadas.Count - 1];
        unidadesGuardadas.RemoveAt(unidadesGuardadas.Count - 1);

        SmileBasico smileBasico = unidad != null ? unidad.GetComponent<SmileBasico>() : null;
        if (smileBasico != null)
        {
            smilesGuardados.Remove(smileBasico);
        }

        if (suctionAudio != null)
        {
            suctionAudio.Play();
        }

        return unidad;
    }

    private void ActivarSmileEnPosicion(SmileBasico smile, Vector3 posicionSalida)
    {
        smile.transform.position = posicionSalida;
        smile.gameObject.SetActive(true);
        ReactivarSmile(smile);
    }

    private void ActivarUnidadEnPosicion(GameObject unidad, Vector3 posicionSalida)
    {
        if (unidad == null)
        {
            return;
        }

        unidad.transform.position = posicionSalida;
        unidad.SetActive(true);

        SmileBasico smile = unidad.GetComponent<SmileBasico>();
        if (smile != null)
        {
            ReactivarSmile(smile);
            return;
        }

        ReactivarSlimeEspecial(unidad);
    }

    private void ReactivarSmile(SmileBasico smile)
    {
        foreach (Collider2D collider in smile.GetComponentsInChildren<Collider2D>(true))
        {
            collider.enabled = true;
        }

        Rigidbody2D rbSmile = smile.GetComponent<Rigidbody2D>();
        if (rbSmile != null)
        {
            rbSmile.simulated = true;
            rbSmile.linearVelocity = Vector2.zero;
        }

        SmileVerdeEvolucion evolucion = smile.GetComponent<SmileVerdeEvolucion>();
        if (evolucion != null)
        {
            evolucion.enabled = true;
        }

        SmileAmarilloEvolucion evolucionAmarilla = smile.GetComponent<SmileAmarilloEvolucion>();
        if (evolucionAmarilla != null)
        {
            evolucionAmarilla.enabled = true;
        }
    }

    private void ReactivarSlimeEspecial(GameObject slime)
    {
        foreach (Collider2D collider in slime.GetComponentsInChildren<Collider2D>(true))
        {
            collider.enabled = true;
            collider.isTrigger = false;
        }

        Rigidbody2D rbSlime = slime.GetComponent<Rigidbody2D>();
        if (rbSlime != null)
        {
            rbSlime.simulated = true;
            rbSlime.linearVelocity = Vector2.zero;
        }

        SetSlimeAiEnabled(slime, true);
    }

    private void LimpiarSmilesGuardadosNulos()
    {
        smilesGuardados.RemoveAll(smile => smile == null);
    }

    private void LimpiarUnidadesGuardadasNulas()
    {
        unidadesGuardadas.RemoveAll(unidad => unidad == null);
        smilesGuardados.RemoveAll(smile => smile == null || !unidadesGuardadas.Contains(smile.gameObject));
    }

    private static void SetSlimeAiEnabled(GameObject slime, bool enabled)
    {
        CleanerSlimeAI cleaner = slime.GetComponent<CleanerSlimeAI>();
        if (cleaner != null)
        {
            cleaner.enabled = enabled;
        }

        ColdSlimeAI cold = slime.GetComponent<ColdSlimeAI>();
        if (cold != null)
        {
            cold.enabled = enabled;
        }

        FireSlimeAI fire = slime.GetComponent<FireSlimeAI>();
        if (fire != null)
        {
            fire.enabled = enabled;
        }

        SlimeController controller = slime.GetComponent<SlimeController>();
        if (controller != null)
        {
            controller.enabled = enabled;
        }
    }

    private void CrearRayoSuccion()
    {
        GameObject rayo = new GameObject("FX_Rayo_Succion");
        rayo.transform.SetParent(transform, false);
        rayoSuccion = rayo.AddComponent<LineRenderer>();
        rayoSuccion.positionCount = 2;
        rayoSuccion.startWidth = 0.22f;
        rayoSuccion.endWidth = 0.04f;
        rayoSuccion.material = new Material(Shader.Find("Sprites/Default"));
        rayoSuccion.startColor = new Color(0.45f, 0.92f, 1f, 0.55f);
        rayoSuccion.endColor = new Color(1f, 1f, 1f, 0.05f);
        rayoSuccion.sortingOrder = 20;
        rayoSuccion.enabled = false;
    }

    private void SpawnParticulasSuccion(Transform objetivo)
    {
        for (int i = 0; i < particulasSuccion; i++)
        {
            GameObject particula = new GameObject("FX_Fotograma_Succion");
            SpriteRenderer renderer = particula.AddComponent<SpriteRenderer>();
            renderer.sprite = particulaSprite;
            renderer.color = new Color(0.55f, 0.93f, 1f, 0.72f);
            renderer.sortingOrder = 21;

            float retraso = Random.Range(0f, duracionSuccion * 0.45f);
            StartCoroutine(MoverParticula(particula.transform, objetivo, retraso));
        }
    }

    private IEnumerator MoverParticula(Transform particula, Transform objetivo, float retraso)
    {
        yield return new WaitForSeconds(retraso);

        Vector3 inicio = objetivo.position + new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(-0.25f, 0.35f), 0f);
        float tiempo = 0f;
        float duracion = Mathf.Max(0.15f, duracionSuccion - retraso);

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            Vector3 destino = bocaSuccionador.position;
            Vector3 curva = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.18f;
            particula.position = Vector3.Lerp(inicio, destino, t) + curva;
            particula.localScale = Vector3.one * Mathf.Lerp(0.18f, 0.04f, t);
            yield return null;
        }

        Destroy(particula.gameObject);
    }

    private void CrearPanelUI()
    {
        Canvas canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == "Canvas_UI") ?? FindFirstObjectByType<Canvas>();

        if (canvas == null || panelUI != null)
        {
            return;
        }

        panelUI = new GameObject("Panel_SubcionadorSmile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        panelUI.transform.SetParent(canvas.transform, false);
        panelUI.layer = canvas.gameObject.layer;

        RectTransform panelRect = panelUI.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-20f, -110f);
        panelRect.sizeDelta = new Vector2(245f, 90f);

        Image panelImage = panelUI.GetComponent<Image>();
        panelImage.sprite = panelSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.raycastTarget = false;

        Shadow shadow = panelUI.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(4f, -5f);

        TMP_Text titulo = CrearTexto(panelUI.transform, "Titulo_Subcionador", "SUBCIONADOR", 15f, TextAlignmentOptions.Center);
        RectTransform tituloRect = titulo.rectTransform;
        tituloRect.anchorMin = new Vector2(0f, 1f);
        tituloRect.anchorMax = new Vector2(1f, 1f);
        tituloRect.pivot = new Vector2(0.5f, 1f);
        tituloRect.anchoredPosition = new Vector2(0f, -10f);
        tituloRect.sizeDelta = new Vector2(-16f, 24f);

        GameObject slot = new GameObject("Slot_SmileGuardado", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        slot.transform.SetParent(panelUI.transform, false);
        slot.layer = canvas.gameObject.layer;
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0f, 0f);
        slotRect.anchorMax = new Vector2(1f, 0f);
        slotRect.pivot = new Vector2(0.5f, 0f);
        slotRect.anchoredPosition = new Vector2(0f, 10f);
        slotRect.sizeDelta = new Vector2(-18f, 46f);

        Image slotImage = slot.GetComponent<Image>();
        slotImage.sprite = slotSprite;
        slotImage.type = Image.Type.Sliced;
        slotImage.raycastTarget = false;

        GameObject iconoGO = new GameObject("Icono_SmileGuardado", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconoGO.transform.SetParent(slot.transform, false);
        iconoSmileUI = iconoGO.GetComponent<Image>();
        iconoSmileUI.preserveAspect = true;
        iconoSmileUI.raycastTarget = false;
        RectTransform iconoRect = iconoGO.GetComponent<RectTransform>();
        iconoRect.anchorMin = new Vector2(0f, 0.5f);
        iconoRect.anchorMax = new Vector2(0f, 0.5f);
        iconoRect.pivot = new Vector2(0f, 0.5f);
        iconoRect.anchoredPosition = new Vector2(10f, 0f);
        iconoRect.sizeDelta = new Vector2(36f, 36f);

        nombreSmileUI = CrearTexto(slot.transform, "Texto_SmileGuardado", "Vacio", 15f, TextAlignmentOptions.MidlineLeft);
        RectTransform nombreRect = nombreSmileUI.rectTransform;
        nombreRect.anchorMin = new Vector2(0f, 0f);
        nombreRect.anchorMax = new Vector2(1f, 1f);
        nombreRect.offsetMin = new Vector2(56f, 16f);
        nombreRect.offsetMax = new Vector2(-8f, -4f);

        estadoSmileUI = CrearTexto(slot.transform, "Texto_AyudaSubcionador", "R: absorber", 11f, TextAlignmentOptions.MidlineLeft);
        RectTransform estadoRect = estadoSmileUI.rectTransform;
        estadoRect.anchorMin = new Vector2(0f, 0f);
        estadoRect.anchorMax = new Vector2(1f, 0f);
        estadoRect.pivot = new Vector2(0.5f, 0f);
        estadoRect.offsetMin = new Vector2(56f, 4f);
        estadoRect.offsetMax = new Vector2(-8f, 18f);
    }

    private TMP_Text CrearTexto(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        go.transform.SetParent(parent, false);
        TMP_Text label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.color = new Color(0.96f, 0.98f, 0.92f, 1f);
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        label.fontSizeMax = size;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.raycastTarget = false;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1.2f, -1.2f);
        return label;
    }

    private void ActualizarPanelUI(Sprite icono = null)
    {
        if (panelUI == null)
        {
            return;
        }

        LimpiarUnidadesGuardadasNulas();

        if (CantidadSmilesGuardados == 0)
        {
            iconoSmileUI.enabled = false;
            nombreSmileUI.text = "Guardados x0";
            estadoSmileUI.text = "R: absorber";
            return;
        }

        GameObject ultimaUnidad = unidadesGuardadas[unidadesGuardadas.Count - 1];
        SmileBasico ultimoSmile = ultimaUnidad != null ? ultimaUnidad.GetComponent<SmileBasico>() : null;
        int verdes = ContarSmilesGuardados(TipoSemilla.Verde);
        int amarillos = ContarSmilesGuardados(TipoSemilla.Amarilla);
        int especiales = ContarSlimesEspecialesGuardados();

        iconoSmileUI.enabled = true;
        iconoSmileUI.sprite = icono != null ? icono : ObtenerSpriteUnidad(ultimaUnidad);
        nombreSmileUI.text = ObtenerNombreUnidad(ultimaUnidad);
        estadoSmileUI.text = "V:" + verdes + " A:" + amarillos + " Esp:" + especiales;
    }

    private int ContarSmilesGuardados(TipoSemilla tipo)
    {
        int total = 0;
        foreach (SmileBasico smile in smilesGuardados)
        {
            if (smile != null && smile.alimentoFavorito == tipo)
            {
                total++;
            }
        }

        return total;
    }

    private Sprite ObtenerSpriteSmile(SmileBasico smile)
    {
        SpriteRenderer renderer = smile.GetComponentInChildren<SpriteRenderer>(true);
        return renderer != null ? renderer.sprite : particulaSprite;
    }

    private int ContarSlimesEspecialesGuardados()
    {
        int total = 0;
        foreach (GameObject unidad in unidadesGuardadas)
        {
            if (unidad != null && unidad.GetComponent<SlimeController>() != null && unidad.GetComponent<SmileBasico>() == null)
            {
                total++;
            }
        }

        return total;
    }

    private Sprite ObtenerSpriteUnidad(GameObject unidad)
    {
        if (unidad == null)
        {
            return particulaSprite;
        }

        SpriteRenderer renderer = unidad.GetComponentInChildren<SpriteRenderer>(true);
        return renderer != null && renderer.sprite != null ? renderer.sprite : particulaSprite;
    }

    private string ObtenerNombreUnidad(GameObject unidad)
    {
        if (unidad == null)
        {
            return "Vacio";
        }

        SmileBasico smile = unidad.GetComponent<SmileBasico>();
        if (smile != null)
        {
            return smile.nombreSmile;
        }

        SlimeController slime = unidad.GetComponent<SlimeController>();
        if (slime != null)
        {
            return slime.SlimeName;
        }

        if (unidad.GetComponent<CleanerSlimeAI>() != null)
        {
            return "Cleaner Slime";
        }

        if (unidad.GetComponent<ColdSlimeAI>() != null)
        {
            return "Cold Slime";
        }

        if (unidad.GetComponent<FireSlimeAI>() != null)
        {
            return "Fire Slime";
        }

        return unidad.name;
    }

    private Transform BuscarHijoPorNombre(Transform root, string parteNombre)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.ToLower().Contains(parteNombre.ToLower()))
            {
                return child;
            }
        }

        return null;
    }

    private void CrearSpritesUI()
    {
        if (panelSprite != null)
        {
            return;
        }

        panelSprite = CrearSpriteRedondeado(PanelFill, PanelBorder, 96, 14, 3);
        slotSprite = CrearSpriteRedondeado(SlotFill, SlotBorder, 64, 10, 2);
        particulaSprite = CrearSpriteRedondeado(Color.white, Color.white, 32, 14, 0);
    }

    private Sprite CrearSpriteRedondeado(Color fill, Color border, int size, int radius, int borderWidth)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float half = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - center) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - center) - (half - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = radius - dist;
                texture.SetPixel(x, y, edge < 0f ? Color.clear : borderWidth > 0 && edge < borderWidth ? border : fill);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private void OnDrawGizmosSelected()
    {
        Transform origen = bocaSuccionador != null ? bocaSuccionador : transform;
        Gizmos.color = new Color(0.45f, 0.92f, 1f, 0.35f);
        Gizmos.DrawWireSphere(origen.position, radioSuccion);
    }
}
