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

    [Header("Costes de Terreno")]
    public int costeUrbano = 1;
    public int costeBosque = 1;
    public int costePradera = 1;
    public int costeCamino = 1;

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

    // Las estadísticas ahora se configuran exclusivamente desde el Inspector de Unity.
    // Para crear distintos tipos de unidades, crea distintos Prefabs y ajusta sus valores allí.

    public bool NecesitaCuracion()
    {
        return vidaActual < vidaMax;
    }

    public int ObtenerCosteTerreno(int biomaID)
    {
        // Usamos los IDs que configures en el Inspector del GridManager
        switch (biomaID)
        {
            case 1: return costeCamino;
            case 2: return costeBosque;
            case 3: return costeUrbano;
            default: return costePradera; // 0 o cualquier ID no registrado
        }
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