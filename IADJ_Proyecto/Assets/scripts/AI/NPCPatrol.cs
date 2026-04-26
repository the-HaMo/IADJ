using System.Collections.Generic;
using UnityEngine;

public class NPCPatrol : MonoBehaviour
{
    [Header("Waypoints por Bando")]
    public List<Transform> waypointsRojo = new List<Transform>();
    public List<Transform> waypointsAzul = new List<Transform>();

    [Header("Configuracion")]
    public float distanciaLlegada = 2f;

    private PathFollowing pathFollowing;
    private NPCStats stats;
    private Pathfinding pathfinder;
    private AgentNPC agent;

    private List<Transform> listaActual;
    private int indexActual = 0;

    void Awake()
    {
        pathFollowing = GetComponent<PathFollowing>();
        stats = GetComponent<NPCStats>();
        pathfinder = FindFirstObjectByType<Pathfinding>();
        agent = GetComponent<AgentNPC>();

        // Empieza desactivado; estadoNPC lo activa si el estado es Vigilancia
        this.enabled = false;
    }

    // Se ejecuta cuando estadoNPC hace: patrol.enabled = true
    void OnEnable()
    {
        if (stats.miBando == Bando.Rojo)
        {
            listaActual = waypointsRojo;
        }
        else
        {
            listaActual = waypointsAzul;
        }

        if (listaActual == null || listaActual.Count == 0)
        {
            return;
        }

        Debug.Log(name + " reanudando patrulla en waypoint " + indexActual);

        // Calculamos la ruta al waypoint actual (solo una vez al reanudar)
        IrAlWaypointActual();
    }

    void Update()
    {
        if (listaActual == null || listaActual.Count == 0)
        {
            return;
        }

        // PathFollowing se desactiva solo cuando termina el camino A*
        // Ese es el momento de avanzar al siguiente waypoint
        if (pathFollowing.enabled == false)
        {
            indexActual++;

            if (indexActual >= listaActual.Count)
            {
                indexActual = 0;
            }

            IrAlWaypointActual();
        }
    }

    private void IrAlWaypointActual()
    {
        if (pathfinder == null || listaActual == null || listaActual.Count == 0)
        {
            return;
        }

        Vector3 posDestino = listaActual[indexActual].position;
        List<Node> camino = pathfinder.FindPath(transform.position, posDestino, stats);

        if (camino != null)
        {
            if (camino.Count > 0)
            {
                pathFollowing.SetPath(camino);
            }
        }
    }

    public void DetenerPatrulla()
    {
        if (pathFollowing != null)
        {
            pathFollowing.FinalizarMovimiento();
        }
    }
}
