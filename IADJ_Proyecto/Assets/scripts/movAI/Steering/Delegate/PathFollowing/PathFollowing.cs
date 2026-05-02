using System.Collections.Generic;
using UnityEngine;

public class PathFollowing : SteeringBehaviour
{
    public List<Transform> waypoints = new List<Transform>();
    public float arrivalRadius = 1.5f;
    public bool loop = false; // Permite patrullas cíclicas
    public bool autoDestroyWaypoints = true; // Si es true, destruye los waypoints al finalizar (útil para A*)

    // El debug de caminos se controla desde EstadoTacticoGlobal (tecla B)
    private int currentWaypointIndex = 0;
    private Agent virtualTarget;
    private Arrive arriveBehaviour;

    void Awake()
    {
        this.nameSteering = "PathFollowing";
        virtualTarget = Agent.CreateStaticVirtual(Vector3.zero, 0.1f, arrivalRadius, 0f, false);
        arriveBehaviour = gameObject.AddComponent<Arrive>();
        arriveBehaviour.enabled = false;
        arriveBehaviour.target = virtualTarget;
        arriveBehaviour.timeToTarget = 0.1f;
    }

    void Update()
    {
        // Debug controlado globalmente por la tecla B (TacticalCanvasController -> EstadoTacticoGlobal)
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        if (waypoints == null || waypoints.Count == 0) return new Steering();

        if (currentWaypointIndex >= waypoints.Count) currentWaypointIndex = waypoints.Count - 1;

        Transform targetWP = waypoints[currentWaypointIndex];
        float dist = Vector3.Distance(agent.Position, targetWP.position);
        bool esUltimo = (currentWaypointIndex == waypoints.Count - 1);

        // ¿Hemos llegado a este waypoint?
        if (dist <= arrivalRadius)
        {
            if (!esUltimo)
            {
                currentWaypointIndex++;
            }
            else if (loop)
            {
                currentWaypointIndex = 0;
            }
            else
            {
                FinalizarMovimiento(agent);
                return new Steering();
            }

            targetWP = waypoints[currentWaypointIndex];
            esUltimo = (currentWaypointIndex == waypoints.Count - 1);
        }

        // Apuntamos el target virtual al waypoint actual
        virtualTarget.Position = targetWP.position;

        // Arrive para TODOS los waypoints para evitar overshoot a alta velocidad.
        // Intermedios: radio grande -> frena poco pero siempre los detecta.
        // Último: radio normal -> frena suavemente hasta parar.
        if (esUltimo == true)
        {
            virtualTarget.ArrivalRadius = arrivalRadius;
        }
        else
        {
            virtualTarget.ArrivalRadius = arrivalRadius * 3f;
        }
        
        virtualTarget.InteriorRadius = 0.1f;

        return arriveBehaviour.GetSteering(agent);
    }

    public void SetPath(List<Node> nodos)
    {
        if (autoDestroyWaypoints)
        {
            foreach (Transform wp in waypoints) { if (wp != null) Destroy(wp.gameObject); }
        }
        waypoints.Clear();

        GameObject pathParent = GameObject.Find("Camino_A*");
        if (pathParent == null) pathParent = new GameObject("Camino_A*");

        foreach (Node n in nodos)
        {
            GameObject go = new GameObject("WP_" + waypoints.Count);
            go.transform.position = n.worldPosition;
            go.transform.SetParent(pathParent.transform);
            waypoints.Add(go.transform);
        }
        currentWaypointIndex = 0;
        loop = false;
        autoDestroyWaypoints = true; // El A* siempre debe auto-destruir sus WPs temporales
        this.enabled = true;
    }

    public void SetPatrol(List<Transform> patrolWaypoints)
    {
        if (autoDestroyWaypoints)
        {
            foreach (Transform wp in waypoints) { if (wp != null) Destroy(wp.gameObject); }
        }
        
        waypoints = new List<Transform>(patrolWaypoints);
        currentWaypointIndex = 0;
        loop = true;
        autoDestroyWaypoints = false; // Las patrullas suelen usar WPs persistentes en la escena
        this.enabled = true;
    }

    public void FinalizarMovimiento(AgentNPC agent = null)
    {
        if (autoDestroyWaypoints)
        {
            foreach (Transform wp in waypoints) { if (wp != null) Destroy(wp.gameObject); }
            waypoints.Clear();
        }
        currentWaypointIndex = 0;

        // Paramos movimiento lineal Y angular para evitar que sigan girando al parar
        if (agent != null)
        {
            agent.Velocity = Vector3.zero;
            agent.Rotation = 0f;
        }
        this.enabled = false;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        bool esPatrulla = false;
        NPCPatrol patrol = GetComponent<NPCPatrol>();
        if (patrol != null && patrol.enabled) esPatrulla = true;

        bool mostrarPorDebug = EstadoTacticoGlobal.DebugActivo;
        // Ahora permitimos que el camino de las unidades se vea siempre (si no son patrullas),
        // independientemente de si el modo táctico está activo o no.
        bool mostrarCaminoUnidad = !esPatrulla;

        if (!mostrarPorDebug && !mostrarCaminoUnidad) return;

        Color colorActual;
        Color colorCamino;
        Color colorPuntoNormal;
        Color colorPuntoDestino;

        if (esPatrulla)
        {
            colorActual = Color.black;
            colorCamino = Color.black;
            colorPuntoNormal = Color.black;
            colorPuntoDestino = Color.black;
        }
        else if (EsUnidadSeleccionada())
        {
            colorActual = Color.yellow;
            colorCamino = Color.yellow;
            colorPuntoNormal = Color.yellow;
            colorPuntoDestino = Color.yellow;
        }
        else if (EstadoTacticoGlobal.PathfindingTacticoActivo)
        {
            colorActual = new Color(1f, 0.35f, 0.35f, 1f);
            colorCamino = Color.red;
            colorPuntoNormal = new Color(1f, 0.35f, 0.35f, 1f);
            colorPuntoDestino = Color.red;
        }
        else
        {
            colorActual = Color.yellow;
            colorCamino = Color.cyan;
            colorPuntoNormal = Color.blue;
            colorPuntoDestino = Color.green;
        }

        // Línea desde el NPC al waypoint actual
        if (currentWaypointIndex < waypoints.Count && waypoints[currentWaypointIndex] != null)
        {
            Gizmos.color = colorActual;
            Gizmos.DrawLine(transform.position, waypoints[currentWaypointIndex].position);
        }

        // Líneas del camino completo
        Gizmos.color = colorCamino;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Vector3 a = waypoints[i].position;
                Vector3 b = waypoints[i + 1].position;

                if (EstadoTacticoGlobal.PathfindingTacticoActivo)
                {
                    DrawThickLine(a, b, 0.12f);
                }
                else
                {
                    Gizmos.DrawLine(a, b);
                }
            }
        }

        // Puntos: verde = destino actual, azul = resto
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.color = (i == currentWaypointIndex) ? colorPuntoDestino : colorPuntoNormal;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
        }
    }

    private void DrawThickLine(Vector3 start, Vector3 end, float thickness)
    {
        Vector3 dir = (end - start).normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * thickness;
        Vector3 up = Vector3.up * (thickness * 0.6f);

        Gizmos.DrawLine(start, end);
        Gizmos.DrawLine(start + side, end + side);
        Gizmos.DrawLine(start - side, end - side);
        Gizmos.DrawLine(start + up, end + up);
        Gizmos.DrawLine(start - up, end - up);
    }

    private bool EsUnidadSeleccionada()
    {
        AgentNPC seleccionada = SeleccionarUnidad.UnidadSeleccionadaActual;
        return seleccionada != null && seleccionada.gameObject == gameObject;
    }
}