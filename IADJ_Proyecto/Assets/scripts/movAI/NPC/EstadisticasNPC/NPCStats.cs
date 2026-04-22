using UnityEngine;

public enum TipoUnidad { Arquero, Caballero, Tanque, Lancero, JineteExplorador }
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

    private void Awake()
    {
        AplicarStatsPorTipo();
        vidaActual = vidaMax;
    }

    private void OnValidate()
    {
        AplicarStatsPorTipo();
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