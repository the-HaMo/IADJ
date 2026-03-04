using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Separation : SteeringBehaviour
{
    [Header("Separation Parameters")]
    public float desiredSeparation = 1.8f;
    public float decayCoefficient = 1.0f;
    public int maxNeighbours = 8;

    void Awake()
    {
        this.nameSteering = "Separation";
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        Collider[] nearby = Physics.OverlapSphere(
            agent.Position,
            desiredSeparation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        int neighbours = 0;
        Vector3 separationForce = Vector3.zero;

        foreach (Collider col in nearby)
        {
            AgentNPC other = col.GetComponent<AgentNPC>();
            if (other == null || other == agent)
            {
                continue;
            }

            Vector3 diff = agent.Position - other.Position;
            float dist = diff.magnitude;
            if (dist <= 0.001f || dist > desiredSeparation)
            {
                continue;
            }

            float strength = Mathf.Min(agent.MaxAcceleration, decayCoefficient / (dist * dist));
            separationForce += diff.normalized * strength;
            neighbours++;

            if (neighbours >= maxNeighbours)
            {
                break;
            }
        }

        if (neighbours == 0)
        {
            return steer;
        }

        steer.linear = Vector3.ClampMagnitude(separationForce, agent.MaxAcceleration);
        steer.angular = 0f;
        return steer;
    }
}
