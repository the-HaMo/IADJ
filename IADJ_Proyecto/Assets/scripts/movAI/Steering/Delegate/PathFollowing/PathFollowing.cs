using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollowing : SteeringBehaviour
{
    [Header("Path Configuration")]
    [Tooltip("Lista de waypoints a seguir")]
    public List<Transform> waypoints = new List<Transform>();
    
    [Tooltip("Radio para considerar que hemos llegado a un waypoint")]
    public float arrivalRadius = 1f;
    
    [Tooltip("Usar Arrive en el último waypoint")]
    public bool useArriveAtEnd = true;

    [Header("Comportamiento al final del camino")]
    public PathEndBehavior endBehavior = PathEndBehavior.Stop;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    // Variables internas
    private int currentWaypointIndex = 0;
    
    // Agentes virtuales para delegar
    private Agent virtualTargetSeek;
    private Agent virtualTargetArrive;
    
    private Seek seekBehavior;
    private Arrive arriveBehavior;

    public enum PathEndBehavior
    {
        Stop,      // Se detiene en el último waypoint
        Loop,      // Vuelve al inicio
        PingPong   // Invierte dirección (patrulla)
    }

    void Awake()
    {
        this.nameSteering = "PathFollowing";
        
        // Crear agentes virtuales para los targets
        virtualTargetSeek = Agent.CreateStaticVirtual(Vector3.zero, 0.5f, 1f, 0f, false);
        virtualTargetArrive = Agent.CreateStaticVirtual(Vector3.zero, 0.5f, 1f, 0f, false);
        
        // Crear instancias de los behaviors delegados
        seekBehavior = gameObject.AddComponent<Seek>();
        seekBehavior.enabled = false;
        seekBehavior.Target = virtualTargetSeek;
        
        arriveBehavior = gameObject.AddComponent<Arrive>();
        arriveBehavior.enabled = false;
        arriveBehavior.target = virtualTargetArrive;
    }

    void OnDestroy()
    {
        // Limpiar los agentes virtuales cuando se destruya el componente
        if (virtualTargetSeek != null && virtualTargetSeek.gameObject != null)
        {
            Destroy(virtualTargetSeek.gameObject);
        }
        if (virtualTargetArrive != null && virtualTargetArrive.gameObject != null)
        {
            Destroy(virtualTargetArrive.gameObject);
        }
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        // Validaciones
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogWarning($"{nameSteering}: No hay waypoints asignados");
            return steer;
        }

        // Limpiar waypoints nulos
        waypoints.RemoveAll(wp => wp == null);
        if (waypoints.Count == 0)
        {
            Debug.LogWarning($"{nameSteering}: Todos los waypoints son nulos");
            return steer;
        }

        // Asegurar que el índice es válido
        if (currentWaypointIndex >= waypoints.Count)
        {
            currentWaypointIndex = waypoints.Count - 1;
        }

        // Obtener el waypoint actual y validar
        Transform currentWaypoint = waypoints[currentWaypointIndex];
        
        if (currentWaypoint == null)
        {
            Debug.LogError($"{nameSteering}: El waypoint en índice {currentWaypointIndex} es nulo");
            return steer;
        }
        
        // Calcular distancia al waypoint actual
        float distanceToWaypoint = Vector3.Distance(agent.Position, currentWaypoint.position);

        // ¿Hemos llegado al waypoint?
        if (distanceToWaypoint <= arrivalRadius)
        {
            // Avanzar al siguiente waypoint
            currentWaypointIndex++;

            // Manejar el final del camino
            if (currentWaypointIndex >= waypoints.Count)
            {
                switch (endBehavior)
                {
                    case PathEndBehavior.Stop:
                        // Quedarse en el último waypoint
                        currentWaypointIndex = waypoints.Count - 1;
                        currentWaypoint = waypoints[currentWaypointIndex];
                        
                        if (currentWaypoint == null)
                        {
                            Debug.LogError($"{nameSteering}: Waypoint final es nulo");
                            return steer;
                        }
                        
                        if (useArriveAtEnd)
                        {
                            // Actualizar posición del agente virtual
                            virtualTargetArrive.Position = currentWaypoint.position;
                            return arriveBehavior.GetSteering(agent);
                        }
                        
                        return steer;

                    case PathEndBehavior.Loop:
                        currentWaypointIndex = 0;
                        currentWaypoint = waypoints[currentWaypointIndex];
                        break;

                    case PathEndBehavior.PingPong:
                        waypoints.Reverse();
                        currentWaypointIndex = 1;
                        
                        if (currentWaypointIndex >= waypoints.Count)
                        {
                            currentWaypointIndex = 0;
                        }
                        currentWaypoint = waypoints[currentWaypointIndex];
                        break;
                }
            }
            else
            {
                currentWaypoint = waypoints[currentWaypointIndex];
            }
            
            if (currentWaypoint == null)
            {
                Debug.LogError($"{nameSteering}: Nuevo waypoint en índice {currentWaypointIndex} es nulo");
                return steer;
            }
        }

        // Si estamos en el último waypoint y el comportamiento es Stop
        if (endBehavior == PathEndBehavior.Stop && 
            currentWaypointIndex == waypoints.Count - 1 && 
            useArriveAtEnd)
        {
            if (currentWaypoint != null)
            {
                // Actualizar posición del agente virtual
                virtualTargetArrive.Position = currentWaypoint.position;
                return arriveBehavior.GetSteering(agent);
            }
            else
            {
                Debug.LogError($"{nameSteering}: No se puede usar Arrive, waypoint es nulo");
                return steer;
            }
        }

        // Caso normal: Seek al waypoint actual
        if (currentWaypoint != null)
        {
            // Actualizar posición del agente virtual
            virtualTargetSeek.Position = currentWaypoint.position;
            return seekBehavior.GetSteering(agent);
        }
        else
        {
            Debug.LogError($"{nameSteering}: No se puede usar Seek, waypoint es nulo");
            return steer;
        }
    }

    // Método público para obtener el waypoint actual (útil para Face)
    public Vector3 GetCurrentTarget()
    {
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
            return transform.position;
        
        Transform currentWaypoint = waypoints[currentWaypointIndex];
        if (currentWaypoint == null)
            return transform.position;
        
        return currentWaypoint.position;
    }

    // Método para obtener el agente virtual del target actual (útil para Face con Agent)
    public Agent GetCurrentTargetAgent()
    {
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
            return null;
        
        Transform currentWaypoint = waypoints[currentWaypointIndex];
        if (currentWaypoint == null)
            return null;
        
        // Actualizar y devolver el agente virtual
        virtualTargetSeek.Position = currentWaypoint.position;
        return virtualTargetSeek;
    }

    // Método para resetear el camino
    public void ResetPath()
    {
        currentWaypointIndex = 0;
    }

    // Método para cambiar waypoints en runtime
    public void SetWaypoints(List<Transform> newWaypoints)
    {
        waypoints = newWaypoints;
        currentWaypointIndex = 0;
    }

    // Método para obtener el índice actual (útil para debugging)
    public int GetCurrentWaypointIndex()
    {
        return currentWaypointIndex;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || waypoints == null || waypoints.Count == 0) return;

        // Limpiar nulos para el dibujado
        List<Transform> validWaypoints = new List<Transform>();
        foreach (var wp in waypoints)
        {
            if (wp != null) validWaypoints.Add(wp);
        }

        if (validWaypoints.Count == 0) return;

        // Dibujar conexiones entre waypoints
        Gizmos.color = Color.blue;
        for (int i = 0; i < validWaypoints.Count - 1; i++)
        {
            Gizmos.DrawLine(validWaypoints[i].position, validWaypoints[i + 1].position);
            DrawArrow(validWaypoints[i].position, validWaypoints[i + 1].position);
        }

        // Si es Loop, conectar el último con el primero
        if (endBehavior == PathEndBehavior.Loop && validWaypoints.Count > 1)
        {
            Gizmos.DrawLine(validWaypoints[validWaypoints.Count - 1].position, validWaypoints[0].position);
            DrawArrow(validWaypoints[validWaypoints.Count - 1].position, validWaypoints[0].position);
        }

        // Dibujar todos los waypoints
        for (int i = 0; i < validWaypoints.Count; i++)
        {
            // Color diferente para el waypoint actual
            Gizmos.color = (i == currentWaypointIndex) ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(validWaypoints[i].position, 0.5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                validWaypoints[i].position + Vector3.up, 
                $"WP {i}",
                new GUIStyle() { normal = new GUIStyleState() { textColor = Color.white } }
            );
            #endif
        }

        // Dibujar radio de llegada en el waypoint actual
        if (currentWaypointIndex < validWaypoints.Count)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(validWaypoints[currentWaypointIndex].position, arrivalRadius);
        }

        // Dibujar línea desde el agente al waypoint actual
        Agent agent = GetComponent<Agent>();
        if (agent != null && currentWaypointIndex < validWaypoints.Count)
        {
            Transform waypoint = validWaypoints[currentWaypointIndex];
            if (waypoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(agent.Position, waypoint.position);
            }
        }
    }

    private void DrawArrow(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        Vector3 midPoint = (from + to) * 0.5f;
        Vector3 right = Vector3.Cross(direction, Vector3.up) * 0.3f;
        
        Gizmos.DrawLine(midPoint, midPoint - direction * 0.3f + right);
        Gizmos.DrawLine(midPoint, midPoint - direction * 0.3f - right);
    }
}