using System.Collections.Generic;
using UnityEngine;

public class SeleccionarUnidad : MonoBehaviour
{
    [Header("Detección de Unidades")]
    public LayerMask capaNPC; 
    
    [Header("Feedback Visual")]
    public GameObject prefabCirculoSeleccion;
    public float alturaGlobalCirculo = 0.3f;

    [Header("Debug Vida (Unidad Seleccionada)")]
    public bool debugVidaSeleccion = true;
    public KeyCode teclaDanioSeleccion = KeyCode.J;
    public KeyCode teclaCuracionSeleccion = KeyCode.K;
    public float debugDanioSeleccion = 10f;
    public float debugCuracionSeleccion = 10f;
    
    private AgentNPC unidadSeleccionada;
    private GameObject instanciaCirculo;
    private Pathfinding buscadorCaminos;

    void Awake()
    {
        // Buscamos el buscador de caminos en la escena
        buscadorCaminos = FindFirstObjectByType<Pathfinding>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ProcesarClick();
        }

        // Tecla C para activar/desactivar visualización global de caminos
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (buscadorCaminos != null)
            {
                buscadorCaminos.mostrarCaminosEnEscena = !buscadorCaminos.mostrarCaminosEnEscena;
                Debug.Log("Visualización de rutas: " + (buscadorCaminos.mostrarCaminosEnEscena ? "ACTIVADA" : "DESACTIVADA"));
            }
        }

        if (debugVidaSeleccion && unidadSeleccionada != null)
        {
            NPCStats stats = unidadSeleccionada.GetComponent<NPCStats>();
            if (stats != null)
            {
                if (Input.GetKeyDown(teclaDanioSeleccion))
                {
                    stats.RecibirDanio(debugDanioSeleccion);
                    Debug.Log($"[Debug Vida] {unidadSeleccionada.name} recibe {debugDanioSeleccion}. Vida: {stats.VidaActual}/{stats.VidaMax}");
                }

                if (Input.GetKeyDown(teclaCuracionSeleccion))
                {
                    stats.RecibirCuracion(debugCuracionSeleccion);
                    Debug.Log($"[Debug Vida] {unidadSeleccionada.name} se cura {debugCuracionSeleccion}. Vida: {stats.VidaActual}/{stats.VidaMax}");
                }
            }
        }

        // Mantener el círculo en la posición de la unidad pero a una altura global fija
        if (unidadSeleccionada != null && instanciaCirculo != null)
        {
            Vector3 posUnidad = unidadSeleccionada.transform.position;
            instanciaCirculo.transform.position = new Vector3(posUnidad.x, alturaGlobalCirculo, posUnidad.z);
            instanciaCirculo.transform.rotation = Quaternion.identity; // Siempre plano
        }
    }

    void ProcesarClick()
    {
        Ray rayo = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(rayo, out hit, 500f, capaNPC))
        {
            AgentNPC unidad = hit.collider.GetComponent<AgentNPC>();
            if (unidad != null)
            {
                if (unidadSeleccionada == unidad) Deseleccionar();
                else Seleccionar(unidad);
                return;
            }
        }

        if (unidadSeleccionada != null)
        {
            Plane planoSuelo = new Plane(Vector3.up, Vector3.zero);
            float distanciaAlSuelo;
            if (planoSuelo.Raycast(rayo, out distanciaAlSuelo))
            {
                MoverUnidadADestino(rayo.GetPoint(distanciaAlSuelo));
            }
        }
    }

    void Seleccionar(AgentNPC unidad)
    {
        unidadSeleccionada = unidad;
        if (prefabCirculoSeleccion != null)
        {
            if (instanciaCirculo == null) instanciaCirculo = Instantiate(prefabCirculoSeleccion);
            instanciaCirculo.SetActive(true);
        }
    }

    void Deseleccionar()
    {
        unidadSeleccionada = null;
        if (instanciaCirculo != null) instanciaCirculo.SetActive(false);
    }

    void MoverUnidadADestino(Vector3 destino)
    {
        if (unidadSeleccionada == null) return;
        
        PercepcionNPC percepcion = unidadSeleccionada.GetComponent<PercepcionNPC>();
        if (percepcion != null)
        {
            percepcion.AsignarOrdenManual(destino);
            return;
        }

        // Fallback
        if (buscadorCaminos == null)
        {
            Debug.LogError("¡No se ha encontrado el componente Pathfinding (A*) en la escena!");
            return;
        }

        NPCStats stats = unidadSeleccionada.GetComponent<NPCStats>();
        List<Node> camino = buscadorCaminos.FindPath(unidadSeleccionada.Position, destino, stats);

        if (camino != null && camino.Count > 0)
        {
            Debug.Log($"[Seleccion] Camino encontrado: {camino.Count} nodos. Enviando a {unidadSeleccionada.name}");
            PathFollowing pf = unidadSeleccionada.GetComponent<PathFollowing>();
            if (pf != null)
            {
                pf.SetPath(camino);
                pf.enabled = true;
                
                // Reiniciamos velocidad para que empiece de cero
                unidadSeleccionada.Velocity = Vector3.zero; 
            }
            else
            {
                Debug.LogError($"[Seleccion] ¡El NPC {unidadSeleccionada.name} no tiene el script PathFollowing!");
            }
        }
        else
        {
            Debug.LogWarning($"[Seleccion] El A* no ha encontrado un camino válido desde {unidadSeleccionada.Position} hasta {destino}");
        }
    }
}
