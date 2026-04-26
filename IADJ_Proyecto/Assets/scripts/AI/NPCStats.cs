using UnityEngine;

public enum Bando { Rojo, Azul }
public enum TipoUnidad { Desconocida = -1, Caballero, Arquero, Lancero, Tanque, Explorador }

public class NPCStats : MonoBehaviour
{
    public event System.Action<float, float, bool> OnVidaCambiada;
    public event System.Action OnDanioRecibido; // Evento exclusivo: el NPC avisa que recibió daño

    [Header("Identidad y Visual")]
    public Bando miBando;
    public TipoUnidad tipoUnidad = TipoUnidad.Desconocida;
    public Material materialRojo, materialAzul;
    public bool crearBarraVida = true;

    [Header("Estadísticas de Combate")]
    public float vidaMax = 1000f;
    public float fuerzaAtaque = 100f;
    public float rangoAtaque = 2f;
    public float velAtaque = 1f;
    public float radioPercepcion = 15f;

    [Header("Costes de Movimiento")]
    public int costeCamino = 1;
    public int costeBosque = 5;
    public int costePradera = 2;
    public int costeUrbano = 1;

    // Propiedades para otros scripts
    public float VidaActual => vidaActual;
    public float VidaMax => vidaMax;
    public float VidaPorcentaje => vidaMax > 0 ? vidaActual / vidaMax : 0;

    private float vidaActual;
    private bool estaEnTratamiento = false; // Nueva bandera para no irse hasta estar al 100%
    private NPCRespawnSpawner respawnSpawner;

    void Awake()
    {
        if (tipoUnidad == TipoUnidad.Desconocida)
        {
            tipoUnidad = InferirTipoUnidadDesdeNombre(gameObject.name);
        }

        vidaActual = vidaMax;
        AplicarMaterial();
        respawnSpawner = FindFirstObjectByType<NPCRespawnSpawner>();
    }

    void Start()
    {
        if (crearBarraVida && !GetComponent<NPCHealthBar>()) 
            gameObject.AddComponent<NPCHealthBar>();
        
        NotificarVida(false);
    }

    // --- Salud ---
    public bool NecesitaCuracion()
    {
        // Si ya estamos en tratamiento, no nos vamos hasta el 100%
        if (estaEnTratamiento)
        {
            if (vidaActual >= vidaMax) { estaEnTratamiento = false; return false; }
            return true;
        }

        // Si tenemos menos del 20%, empezamos el tratamiento
        if (vidaActual < vidaMax * 0.2f)
        {
            estaEnTratamiento = true;
            return true;
        }

        return false;
    }

    public void RecibirDanio(float d)
    {
        vidaActual -= d;
        NotificarVida(true);
        OnDanioRecibido?.Invoke(); // El NPC notifica que recibió daño
        if (vidaActual <= 0)
        {
            if (respawnSpawner == null)
            {
                respawnSpawner = FindFirstObjectByType<NPCRespawnSpawner>();
            }

            if (respawnSpawner != null)
            {
                respawnSpawner.RespawnNPCEnPuntoMasCercano(tipoUnidad, miBando, transform.position);
            }

            Destroy(gameObject);
        }
    }

    public void CopiarStatsDesde(NPCStats origen)
    {
        if (origen == null)
        {
            return;
        }

        miBando = origen.miBando;
        tipoUnidad = origen.tipoUnidad;
        materialRojo = origen.materialRojo;
        materialAzul = origen.materialAzul;
        crearBarraVida = origen.crearBarraVida;

        vidaMax = origen.vidaMax;
        fuerzaAtaque = origen.fuerzaAtaque;
        rangoAtaque = origen.rangoAtaque;
        velAtaque = origen.velAtaque;
        radioPercepcion = origen.radioPercepcion;

        costeCamino = origen.costeCamino;
        costeBosque = origen.costeBosque;
        costePradera = origen.costePradera;
        costeUrbano = origen.costeUrbano;

        vidaActual = vidaMax;
        estaEnTratamiento = false;
        AplicarMaterial();
    }

    public void RecibirCuracion(float c)
    {
        if (vidaActual < vidaMax)
        {
            vidaActual = Mathf.Min(vidaActual + c, vidaMax);
            NotificarVida(true);
        }
    }

    private void NotificarVida(bool mostrar) => OnVidaCambiada?.Invoke(vidaActual, vidaMax, mostrar);

    // --- Utilidades ---
    public void AplicarMaterial()
    {
        Renderer r = GetComponent<Renderer>();
        if (r) r.material = (miBando == Bando.Rojo) ? materialRojo : materialAzul;
    }

    private TipoUnidad InferirTipoUnidadDesdeNombre(string nombre)
    {
        string limpio = nombre.Replace("(Clone)", "").Trim();

        if (limpio.StartsWith("Caballero")) return TipoUnidad.Caballero;
        if (limpio.StartsWith("Arquero")) return TipoUnidad.Arquero;
        if (limpio.StartsWith("Lancero")) return TipoUnidad.Lancero;
        if (limpio.StartsWith("Tanque")) return TipoUnidad.Tanque;
        if (limpio.StartsWith("Explorador")) return TipoUnidad.Explorador;

        return TipoUnidad.Desconocida;
    }

    public int ObtenerCosteTerreno(Bioma bioma)
    {
        return bioma switch {
            Bioma.Bosque => costeBosque,
            Bioma.Pradera => costePradera,
            Bioma.Urbano => costeUrbano,
            _ => costeCamino
        };
    }
}