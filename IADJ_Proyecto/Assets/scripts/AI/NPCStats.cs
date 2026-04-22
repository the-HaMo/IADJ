using UnityEngine;

public enum TipoUnidad { Arquero, Caballero, Tanque, JineteExplorador,Lancero }
public enum Bando { Rojo, Azul }

public class NPCStats : MonoBehaviour
{
    [Header("Identidad")]
    public TipoUnidad miTipoDeUnidad;
    public Bando miBando;
    
    [Header("Estadisticas")]
    public float vidaMax = 100f;
    private float vidaActual;

    public float fuerzaAtaque = 10f;
    public float rangoAtaque = 1.5f;
    public float velAtaque = 1.5f;

    [Header("Costes de Terreno (Biomas)")]
    public int costeUrbano = 1;
    public int costeBosque = 1;
    public int costePradera = 1;
    public int costeCamino = 1;

    public Material materialRojo;
    public Material materialAzul;

    private void Awake()
    {
        AplicarStatsPorTipo();
        vidaActual = vidaMax;
    }

    private void OnValidate()
    {
        AplicarStatsPorTipo();
    }

    private void Start()
    {
        AplicarMaterialBando();
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
        else if (miTipoDeUnidad == TipoUnidad.JineteExplorador)
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

    public void RecibirCuracion(float cantidad)
    {
        vidaActual += cantidad;
        if (vidaActual > vidaMax)
        {
            vidaActual = vidaMax;
        }
    }
}