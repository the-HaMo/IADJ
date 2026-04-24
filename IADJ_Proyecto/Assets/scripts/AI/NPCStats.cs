using UnityEngine;

public enum TipoUnidad { Arquero, Caballero, Tanque,Explorador,Lancero }
public enum Bando { Rojo, Azul }

public class NPCStats : MonoBehaviour
{
    public event System.Action<float, float, bool> OnVidaCambiada;

    [Header("Identidad")]
    public TipoUnidad miTipoDeUnidad;
    public Bando miBando;
    
    [Header("Estadisticas")]
    public float vidaMax = 100f;
    public float vidaActual;

    public float fuerzaAtaque = 10f;
    public float rangoAtaque = 1.5f;
    public float velAtaque = 1.5f;

    [Header("Costes de Terreno")]
    public int costeUrbano = 1;
    public int costeBosque = 1;
    public int costePradera = 1;
    public int costeCamino = 1;

    public Material materialRojo;
    public Material materialAzul;

    [Header("UI Vida")]
    [SerializeField] private bool crearBarraVida = true;

    [Header("Debug Vida")]
    [SerializeField] private float debugDanio = 10f;
    [SerializeField] private float debugCuracion = 10f;

    public float VidaActual => vidaActual;
    public float VidaMax => vidaMax;
    public float VidaPorcentaje => vidaMax > 0f ? vidaActual / vidaMax : 0f;

    private void Awake()
    {
        AplicarStatsPorTipo();
        vidaActual = vidaMax;
        NotificarCambioVida(false);
    }

    private void OnValidate()
    {
        AplicarStatsPorTipo();
    }

    private void Start()
    {
        AplicarMaterialBando();
        if (crearBarraVida)
        {
            EnsureHealthBar();
        }
    }

    private void EnsureHealthBar()
    {
        if (GetComponent<NPCHealthBar>() == null)
        {
            gameObject.AddComponent<NPCHealthBar>();
        }
    }

    public void AplicarMaterialBando()
    {
        Material materialSeleccionado = (miBando == Bando.Rojo) ? materialRojo : materialAzul;
        
        if (materialSeleccionado != null)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = materialSeleccionado;
            }
        }
    }

    private void AplicarStatsPorTipo()
    {
        if (miTipoDeUnidad == TipoUnidad.Arquero)
        {
            vidaMax = 80f;
            fuerzaAtaque = 15f;
            rangoAtaque = 5f;
            velAtaque = 1.2f;
        }
        else if (miTipoDeUnidad == TipoUnidad.Caballero)
        {
            vidaMax = 120f;
            fuerzaAtaque = 20f;
            rangoAtaque = 1.5f;
            velAtaque = 1f;
        }
        else if (miTipoDeUnidad == TipoUnidad.Tanque)
        {
            vidaMax = 150f;
            fuerzaAtaque = 10f;
            rangoAtaque = 1.5f;
            velAtaque = 0.8f;
        }
        else if (miTipoDeUnidad == TipoUnidad.Lancero)
        {
            vidaMax = 90f;
            fuerzaAtaque = 12f;
            rangoAtaque = 2.5f;
            velAtaque = 1.3f;
        }
        else if (miTipoDeUnidad == TipoUnidad.Explorador)
        {
            vidaMax = 70f;
            fuerzaAtaque = 8f;
            rangoAtaque = 3f;
            velAtaque = 1.5f;
        }
    }

    public bool NecesitaCuracion()
    {
        return vidaActual < vidaMax;
    }

    public int ObtenerCosteTerreno(Bioma bioma)
    {
        switch (bioma)
        {
            case Bioma.Camino: return costeCamino;
            case Bioma.Bosque: return costeBosque;
            case Bioma.Urbano: return costeUrbano;
            default: return costePradera;
        }
    }

    public int ObtenerCosteTerreno(int biomaID)
    {
        return ObtenerCosteTerreno((Bioma)biomaID);
    }

    public void RecibirCuracion(float cantidad)
    {
        float vidaAntes = vidaActual;
        vidaActual += cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMax);

        if (vidaActual > vidaAntes)
        {
            NotificarCambioVida(false);
        }
    }

    public void RecibirDanio(float cantidad)
    {
        float vidaAntes = vidaActual;
        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMax);

        if (vidaActual < vidaAntes)
        {
            NotificarCambioVida(true);
        }
    }

    private void NotificarCambioVida(bool fueDanio)
    {
        OnVidaCambiada?.Invoke(vidaActual, vidaMax, fueDanio);
    }

    [ContextMenu("Debug/Aplicar Danio")]
    private void DebugAplicarDanio()
    {
        RecibirDanio(debugDanio);
        Debug.Log($"[{name}] Danio aplicado: {debugDanio}. Vida: {vidaActual}/{vidaMax}");
    }

    [ContextMenu("Debug/Aplicar Curacion")]
    private void DebugAplicarCuracion()
    {
        RecibirCuracion(debugCuracion);
        Debug.Log($"[{name}] Curacion aplicada: {debugCuracion}. Vida: {vidaActual}/{vidaMax}");
    }
}