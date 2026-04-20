using UnityEngine;

public enum TipoUnidad { Arquero, Caballero, Tanque }
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
        vidaActual = vidaMax;
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