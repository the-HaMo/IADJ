using UnityEngine;

public enum TipoUnidad { Arquero, Caballero, Tanque, JineteExplorador,Lancero }
public enum Bando { Rojo, Azul }

public class NPCStats : MonoBehaviour
{
    [Header("Identidad")]
    public TipoUnidad miTipoDeUnidad;
    public Bando miBando;
    
    [Header("Estadisticas")]
    public float vidaMax = 1000f;
    private float vidaActual;

    public float fuerzaAtaque = 100f;
    public float rangoAtaque = 1.5f;
    public float velAtaque = 15f;

    [Header("Costes de Terreno")]
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
            vidaMax = 800f;
            fuerzaAtaque = 150f;
            rangoAtaque = 5f;
            velAtaque = 12f;
        }
        else if (miTipoDeUnidad == TipoUnidad.Caballero)
        {
            vidaMax = 1200f;
            fuerzaAtaque = 200f;
            rangoAtaque = 1.5f;
            velAtaque = 10f;
        }
        else if (miTipoDeUnidad == TipoUnidad.Tanque)
        {
            vidaMax = 1500f;
            fuerzaAtaque = 100f;
            rangoAtaque = 1.5f;
            velAtaque = 8f;
        }
        else if (miTipoDeUnidad == TipoUnidad.Lancero)
        {
            vidaMax = 900f;
            fuerzaAtaque = 120f;
            rangoAtaque = 2.5f;
            velAtaque = 13f;
        }
        else if (miTipoDeUnidad == TipoUnidad.JineteExplorador)
        {
            vidaMax = 700f;
            fuerzaAtaque = 80f;
            rangoAtaque = 3f;
            velAtaque = 15f;
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
        vidaActual += cantidad;
        if (vidaActual > vidaMax)
        {
            vidaActual = vidaMax;
        }
    }
}