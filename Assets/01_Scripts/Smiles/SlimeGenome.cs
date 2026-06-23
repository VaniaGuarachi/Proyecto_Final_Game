using System;
using UnityEngine;

public enum SlimeSpecies
{
    Desconocido,
    Verde,
    Amarillo,
    Cleaner,
    Acid,
    Fire,
    Cold,
    Nature
}

public enum SlimeBiomeType
{
    Neutral,
    Fertil,
    Humedo,
    Toxico,
    Volcanico,
    Frio,
    Cristalino,
    Ruinas
}

public enum SlimeEcologicalState
{
    Estable,
    Hambriento,
    Sobrealimentado,
    Inestable,
    EnRiesgo,
    Evolucionado
}

[DisallowMultipleComponent]
public class SlimeGenome : MonoBehaviour
{
    [Header("Identidad biologica")]
    public SlimeSpecies especie = SlimeSpecies.Desconocido;
    public SlimeBiomeType biomaFavorito = SlimeBiomeType.Neutral;
    public TipoSemilla alimentoFavorito = TipoSemilla.Verde;
    public string traits = "adaptable";

    [Header("Estado biologico")]
    [Range(0f, 100f)] public float estabilidadGenetica = 78f;
    [Range(0f, 100f)] public float energiaInterna = 45f;
    [Range(0f, 100f)] public float masaBiologica = 35f;
    [Range(0f, 100f)] public float felicidad = 50f;
    public SlimeEcologicalState estadoEcologico = SlimeEcologicalState.Estable;

    [Header("Evolucion")]
    public bool evolucionado;
    public float alimentacionesCorrectas;
    public float segundosEnBiomaFavorito;
    public float exposicionBiomaClave;
    public float mutaciones;

    private SlimeBiomeType biomaActual = SlimeBiomeType.Neutral;
    private float tickTimer;

    public SlimeBiomeType BiomaActual => biomaActual;
    public bool EstaEnBiomaFavorito => biomaActual == biomaFavorito || (biomaFavorito == SlimeBiomeType.Frio && biomaActual == SlimeBiomeType.Cristalino);

    private void Awake()
    {
        InferirDatosSiFaltan();
    }

    private void OnEnable()
    {
        SlimeGenomeInstaller.Register(this);
        InferirDatosSiFaltan();
    }

    private void OnDisable()
    {
        SlimeGenomeInstaller.Unregister(this);
    }

    private void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer < 1f)
        {
            return;
        }

        float delta = tickTimer;
        tickTimer = 0f;
        TickBiologico(delta);
    }

    public void InferirDatosSiFaltan()
    {
        especie = InferirEspecie();

        switch (especie)
        {
            case SlimeSpecies.Fire:
                biomaFavorito = SlimeBiomeType.Volcanico;
                alimentoFavorito = TipoSemilla.Amarilla;
                traits = AsegurarTrait("termico,peligroso");
                break;
            case SlimeSpecies.Cold:
                biomaFavorito = SlimeBiomeType.Frio;
                alimentoFavorito = TipoSemilla.Azul;
                traits = AsegurarTrait("cristalino,calmante");
                break;
            case SlimeSpecies.Cleaner:
                biomaFavorito = SlimeBiomeType.Humedo;
                alimentoFavorito = TipoSemilla.Verde;
                traits = AsegurarTrait("purificador,humedo");
                break;
            case SlimeSpecies.Acid:
                biomaFavorito = SlimeBiomeType.Toxico;
                alimentoFavorito = TipoSemilla.Morada;
                traits = AsegurarTrait("corrosivo,inestable");
                break;
            case SlimeSpecies.Nature:
            case SlimeSpecies.Verde:
                biomaFavorito = SlimeBiomeType.Fertil;
                alimentoFavorito = TipoSemilla.Verde;
                traits = AsegurarTrait("biomasa,regenerativo");
                break;
            case SlimeSpecies.Amarillo:
                biomaFavorito = SlimeBiomeType.Humedo;
                alimentoFavorito = TipoSemilla.Azul;
                traits = AsegurarTrait("acuatico,suave");
                break;
            default:
                biomaFavorito = SlimeBiomeType.Neutral;
                alimentoFavorito = TipoSemilla.Verde;
                traits = AsegurarTrait("adaptable");
                break;
        }
    }

    public void RegistrarAlimentacion(TipoSemilla alimento, bool correcta)
    {
        if (correcta)
        {
            alimentacionesCorrectas += 1f;
            energiaInterna = Mathf.Clamp(energiaInterna + 24f, 0f, 100f);
            masaBiologica = Mathf.Clamp(masaBiologica + 18f, 0f, 100f);
            felicidad = Mathf.Clamp(felicidad + 16f, 0f, 100f);
            estabilidadGenetica = Mathf.Clamp(estabilidadGenetica + 4f, 0f, 100f);
            estadoEcologico = energiaInterna > 92f ? SlimeEcologicalState.Sobrealimentado : SlimeEcologicalState.Estable;
        }
        else
        {
            felicidad = Mathf.Clamp(felicidad - 10f, 0f, 100f);
            estabilidadGenetica = Mathf.Clamp(estabilidadGenetica - 7f, 0f, 100f);
            estadoEcologico = SlimeEcologicalState.Inestable;
        }

        BiologicalResearchSystem.RegistrarAlimentacion(this, correcta);
        EcosystemManager.RegistrarEventoAlimentacion(this, correcta);
    }

    public void RegistrarProduccion(string recurso, int cantidad)
    {
        energiaInterna = Mathf.Clamp(energiaInterna - Mathf.Max(1, cantidad) * 2f, 0f, 100f);
        EcosystemManager.RegistrarProduccion(this, recurso, cantidad);
    }

    public void RegistrarEvolucion(string nuevoTrait)
    {
        evolucionado = true;
        estadoEcologico = SlimeEcologicalState.Evolucionado;
        traits = AsegurarTrait(nuevoTrait);
        estabilidadGenetica = Mathf.Clamp(estabilidadGenetica + 12f, 0f, 100f);
        energiaInterna = Mathf.Clamp(energiaInterna - 35f, 15f, 100f);
        BiologicalResearchSystem.RegistrarEvolucion(this);
    }

    public void RegistrarMutacion(string trait)
    {
        mutaciones += 1f;
        traits = AsegurarTrait(trait);
        estabilidadGenetica = Mathf.Clamp(estabilidadGenetica - 12f, 0f, 100f);
        BiologicalResearchSystem.RegistrarMutacion(this);
    }

    public void HeredarDesde(SlimeGenome origen, bool mutar)
    {
        if (origen == null)
        {
            InferirDatosSiFaltan();
            return;
        }

        especie = origen.especie;
        biomaFavorito = origen.biomaFavorito;
        alimentoFavorito = origen.alimentoFavorito;
        traits = origen.traits;
        estabilidadGenetica = Mathf.Clamp(origen.estabilidadGenetica + UnityEngine.Random.Range(-6f, 4f), 0f, 100f);
        energiaInterna = Mathf.Clamp(origen.energiaInterna * 0.45f, 18f, 70f);
        masaBiologica = Mathf.Clamp(origen.masaBiologica * 0.50f, 18f, 65f);
        felicidad = Mathf.Clamp(origen.felicidad - 8f, 0f, 100f);

        if (mutar)
        {
            RegistrarMutacion("mutacion leve");
        }
    }

    public void SetBiomaActual(SlimeBiomeType nuevoBioma)
    {
        if (biomaActual != nuevoBioma && nuevoBioma != SlimeBiomeType.Neutral)
        {
            BiologicalResearchSystem.RegistrarBioma(nuevoBioma);
        }

        biomaActual = nuevoBioma;
    }

    private void TickBiologico(float delta)
    {
        energiaInterna = Mathf.Clamp(energiaInterna - delta * 0.22f, 0f, 100f);
        masaBiologica = Mathf.Clamp(masaBiologica - delta * 0.04f, 0f, 100f);

        if (EstaEnBiomaFavorito)
        {
            segundosEnBiomaFavorito += delta;
            exposicionBiomaClave += delta;
            felicidad = Mathf.Clamp(felicidad + delta * 0.45f, 0f, 100f);
            estabilidadGenetica = Mathf.Clamp(estabilidadGenetica + delta * 0.18f, 0f, 100f);
        }
        else if (biomaActual != SlimeBiomeType.Neutral)
        {
            estabilidadGenetica = Mathf.Clamp(estabilidadGenetica - delta * 0.28f, 0f, 100f);
        }

        if (energiaInterna < 18f)
        {
            estadoEcologico = SlimeEcologicalState.Hambriento;
        }
        else if (estabilidadGenetica < 28f)
        {
            estadoEcologico = SlimeEcologicalState.Inestable;
        }
        else if (!evolucionado)
        {
            estadoEcologico = SlimeEcologicalState.Estable;
        }
    }

    private SlimeSpecies InferirEspecie()
    {
        if (GetComponent<FireSlimeAI>() != null) return SlimeSpecies.Fire;
        if (GetComponent<ColdSlimeAI>() != null) return SlimeSpecies.Cold;
        if (GetComponent<CleanerSlimeAI>() != null) return SlimeSpecies.Cleaner;
        if (GetComponent<AcidSlimeAI>() != null) return SlimeSpecies.Acid;
        if (GetComponent<NatureSlimeAI>() != null) return SlimeSpecies.Nature;
        if (GetComponent<SmileVerdeEvolucion>() != null) return SlimeSpecies.Verde;
        if (GetComponent<SmileAmarilloEvolucion>() != null) return SlimeSpecies.Amarillo;

        SmileBasico smile = GetComponent<SmileBasico>();
        if (smile != null)
        {
            return smile.alimentoFavorito == TipoSemilla.Amarilla ? SlimeSpecies.Amarillo : SlimeSpecies.Verde;
        }

        SlimeController controller = GetComponent<SlimeController>();
        if (controller != null)
        {
            string tipo = (controller.SlimeName + " " + controller.SlimeType).ToLowerInvariant();
            if (tipo.Contains("fire") || tipo.Contains("fuego")) return SlimeSpecies.Fire;
            if (tipo.Contains("cold") || tipo.Contains("frio") || tipo.Contains("cristal")) return SlimeSpecies.Cold;
            if (tipo.Contains("clean") || tipo.Contains("purificador")) return SlimeSpecies.Cleaner;
            if (tipo.Contains("acid") || tipo.Contains("acido")) return SlimeSpecies.Acid;
            if (tipo.Contains("nature") || tipo.Contains("verde")) return SlimeSpecies.Nature;
        }

        return SlimeSpecies.Desconocido;
    }

    private string AsegurarTrait(string trait)
    {
        if (string.IsNullOrWhiteSpace(traits) || traits == "adaptable")
        {
            return trait;
        }

        if (string.IsNullOrWhiteSpace(trait) || traits.IndexOf(trait, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return traits;
        }

        return traits + "," + trait;
    }
}

public static class SlimeGenomeInstaller
{
    private static readonly System.Collections.Generic.HashSet<SlimeGenome> registrados = new System.Collections.Generic.HashSet<SlimeGenome>();

    public static System.Collections.Generic.IEnumerable<SlimeGenome> Registrados => registrados;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        if (UnityEngine.Object.FindFirstObjectByType<SlimeGenomeRuntimeInstaller>() != null)
        {
            return;
        }

        GameObject go = new GameObject("SlimeGenomeInstaller_Runtime");
        go.AddComponent<SlimeGenomeRuntimeInstaller>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    public static void Register(SlimeGenome genome)
    {
        if (genome != null)
        {
            registrados.Add(genome);
        }
    }

    public static void Unregister(SlimeGenome genome)
    {
        if (genome != null)
        {
            registrados.Remove(genome);
        }
    }

    public static SlimeGenome Ensure(GameObject slime)
    {
        if (slime == null)
        {
            return null;
        }

        SlimeGenome genome = slime.GetComponent<SlimeGenome>();
        if (genome == null)
        {
            genome = slime.AddComponent<SlimeGenome>();
        }

        genome.InferirDatosSiFaltan();
        return genome;
    }
}

public class SlimeGenomeRuntimeInstaller : MonoBehaviour
{
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < 1.5f)
        {
            return;
        }

        timer = 0f;
        InstalarEnSlimesExistentes();
    }

    private static void InstalarEnSlimesExistentes()
    {
        foreach (SlimeController slime in FindObjectsByType<SlimeController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            SlimeGenomeInstaller.Ensure(slime.gameObject);
        }

        foreach (SmileBasico smile in FindObjectsByType<SmileBasico>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            SlimeGenomeInstaller.Ensure(smile.gameObject);
        }

        foreach (FireSlimeAI slime in FindObjectsByType<FireSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            SlimeGenomeInstaller.Ensure(slime.gameObject);
        }

        foreach (ColdSlimeAI slime in FindObjectsByType<ColdSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            SlimeGenomeInstaller.Ensure(slime.gameObject);
        }

        foreach (CleanerSlimeAI slime in FindObjectsByType<CleanerSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            SlimeGenomeInstaller.Ensure(slime.gameObject);
        }

        foreach (AcidSlimeAI slime in FindObjectsByType<AcidSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            SlimeGenomeInstaller.Ensure(slime.gameObject);
        }

        foreach (NatureSlimeAI slime in FindObjectsByType<NatureSlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            SlimeGenomeInstaller.Ensure(slime.gameObject);
        }
    }
}
