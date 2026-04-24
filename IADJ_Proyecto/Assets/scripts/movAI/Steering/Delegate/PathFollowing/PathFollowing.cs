using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFollowing : SteeringBehaviour
{
    public List<Transform> waypoints = new List<Transform>();
    public float arrivalRadius = 1.5f;
    public float timeToTarget = 0.1f;
    public bool mostrarGizmos = false;

    private int currentWaypointIndex = 0;
    private Agent virtualTarget;
    private Arrive arriveBehavior;

    void Awake()
    {
        this.nameSteering = "PathFollowing";
        virtualTarget = Agent.CreateStaticVirtual(Vector3.zero, 0.1f, 0.5f, 0f, false);
        arriveBehavior = gameObject.AddComponent<Arrive>();
        arriveBehavior.enabled = false;
        arriveBehavior.target = virtualTarget;
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        if (waypoints == null || waypoints.Count == 0) return new Steering();

        if (currentWaypointIndex >= waypoints.Count) currentWaypointIndex = waypoints.Count - 1;

        Transform targetWP = waypoints[currentWaypointIndex];
        float dist = Vector3.Distance(agent.Position, targetWP.position);

        if (dist <= arrivalRadius)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                foreach (Transform wp in waypoints) { if (wp != null) Destroy(wp.gameObject); }
                waypoints.Clear();
                currentWaypointIndex = 0;
                this.enabled = false; 
                return new Steering();
            }
            targetWP = waypoints[currentWaypointIndex];
        }

        virtualTarget.Position = targetWP.position;
        arriveBehavior.timeToTarget = timeToTarget;
        return arriveBehavior.GetSteering(agent);
    }

    public void SetPath(List<Node> nodos)
    {
        foreach (Transform wp in waypoints) { if (wp != null) Destroy(wp.gameObject); }
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
    }

    private void OnDrawGizmos()
    {
        bool debugGlobal = false;
        Pathfinding pf = FindFirstObjectByType<Pathfinding>();
        if (pf != null) debugGlobal = pf.mostrarCaminosEnEscena;

        if ((!mostrarGizmos && !debugGlobal) || waypoints == null || waypoints.Count == 0) return;

        AgentNPC agent = GetComponent<AgentNPC>();
        if (agent != null && agent.Velocity.sqrMagnitude < 0.1f && currentWaypointIndex < waypoints.Count)
            Gizmos.color = Color.red;
        else
            Gizmos.color = Color.yellow;

        if (currentWaypointIndex < waypoints.Count && waypoints[currentWaypointIndex] != null)
            Gizmos.DrawLine(transform.position, waypoints[currentWaypointIndex].position);

        Gizmos.color = Color.blue;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i+1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.color = (i == currentWaypointIndex) ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
            
            if (i == currentWaypointIndex)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(waypoints[i].position, arrivalRadius);
            }
        }
    }
}