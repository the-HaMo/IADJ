using UnityEngine;

public enum Bando { Rojo, Azul }

public class NPCStats : MonoBehaviour
{
    public event System.Action<float, float, bool> OnVidaCambiada;

    [Header("Identidad y Visual")]
    public Bando miBando;
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

    void Awake()
    {
        vidaActual = vidaMax;
        AplicarMaterial();
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
        if (vidaActual <= 0) Destroy(gameObject);
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
}