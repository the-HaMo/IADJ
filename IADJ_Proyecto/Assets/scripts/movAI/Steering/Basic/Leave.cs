using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leave : SteeringBehaviour
{
    // Declara las variables que necesites para este SteeringBehaviour
    public Agent target;
    public float escapeRadius;

    void Start()
    {
        this.nameSteering = "Leave";
    }

    public override void NewTarget(Agent t)
    {
        this.target = t;
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        if (target == null)
        {
            return new Steering();
        }

        Steering steer = new Steering();
        float distance = (agent.Position - target.Position).magnitude;

        if (distance >= escapeRadius)
        {
            // Frenado completo si el agente ya está fuera del radio de escape
            steer.linear = -agent.Velocity;
            steer.angular = 0f;
            return steer;
        }

        // Si todavía está dentro del radio de escape, huye del objetivo (igual que Flee)
        Vector3 desiredVelocity = (agent.Position - target.Position).normalized * agent.MaxSpeed;
        steer.linear = desiredVelocity - agent.Velocity;
        steer.angular = 0f;
        return steer;
    }
}
