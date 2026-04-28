using UnityEngine;

public enum Bando { Rojo, Azul, Default }
public enum TipoUnidad { Caballero, Arquero, Lancero, Tanque, Explorador }

public class NPCStats : MonoBehaviour
{
    public event System.Action<float, float, bool> OnVidaCambiada;
    public event System.Action OnDanioRecibido; // Evento exclusivo: el NPC avisa que recibió daño
    public event System.Action<NPCStats> OnAtacado; // Igual que el anterior pero pasando el atacante

    [Header("Identidad y Visual")]
    public Bando miBando= Bando.Default;
    public TipoUnidad tipoUnidad = TipoUnidad.Caballero;
    public Material materialRojo, materialAzul;
    public bool crearBarraVida = true;

    [Header("Estadísticas de Combate")]
    public float vidaMax = 1000f;
    public float poder = 100f;
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
    private bool estaEnTratamiento = false;
    private bool estaMuerto = false; // Flag para evitar múltiples muertes en el mismo frame
    private NPCRespawnSpawner respawnSpawner;
    private GridManager gridManager;

    void Awake()
    {
        vidaActual = vidaMax;
        AplicarMaterial();
        respawnSpawner = FindFirstObjectByType<NPCRespawnSpawner>();
        gridManager = FindFirstObjectByType<GridManager>();
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

    public void RecibirDanio(float d, NPCStats atacante = null)
    {
        if (estaMuerto) return; // Si ya ha muerto en este frame, ignoramos el daño extra

        vidaActual -= d;
        NotificarVida(true);
        OnDanioRecibido?.Invoke();      // Evento sin atacante (Torre, etc.)
        OnAtacado?.Invoke(atacante);    // Evento con atacante (PercepcionNPC)
        
        if (vidaActual <= 0)
        {
            estaMuerto = true; // Marcamos como muerto para que otros ataques en este mismo frame no activen el código

            if (respawnSpawner == null)
            {
                respawnSpawner = FindFirstObjectByType<NPCRespawnSpawner>();
            }

            if (respawnSpawner != null)
            {
                respawnSpawner.RegistrarMuerteYRespawn(tipoUnidad, miBando, transform.position);
            }

            Destroy(gameObject);
        }
    }

    // Wrapper que aplica directamente la formula del Anexo II usando SistemaCombate.
    // El atacante llama: defensor.RecibirAtaqueDe(this);
    public SistemaCombate.ResultadoAtaque RecibirAtaqueDe(NPCStats atacante)
    {
        SistemaCombate.ResultadoAtaque res = default;
        if (atacante == null) return res;

        Bioma terrenoAtacante = atacante.ObtenerBiomaActual();
        Bioma terrenoDefensor = ObtenerBiomaActual();

        res = SistemaCombate.CalcularDanio(atacante, terrenoAtacante, this, terrenoDefensor);
        RecibirDanio(res.danio, atacante);
        return res;
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
        poder = origen.poder;
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

    public int ObtenerCosteTerreno(Bioma bioma)
    {
        return bioma switch {
            Bioma.Bosque => costeBosque,
            Bioma.Pradera => costePradera,
            Bioma.Urbano => costeUrbano,
            _ => costeCamino
        };
    }

    // --- Posicion en el mundo ---

    /// <summary>Devuelve el nodo del grid en el que se encuentra actualmente este NPC.</summary>
    public Node ObtenerNodoActual()
    {
        if (gridManager == null) return null;
        return gridManager.NodeFromWorldPoint(transform.position);
    }

    /// <summary>Devuelve el bioma en el que se encuentra actualmente este NPC.</summary>
    public Bioma ObtenerBiomaActual()
    {
        Node nodo = ObtenerNodoActual();
        return (nodo != null) ? nodo.bioma : Bioma.Pradera;
    }
}