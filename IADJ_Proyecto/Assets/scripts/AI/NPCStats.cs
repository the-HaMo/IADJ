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

    public Material materialRojo;
    public Material materialAzul;

    private void Awake()
    {
        vidaActual = vidaMax;
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