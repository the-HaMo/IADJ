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

        // Si el agente ya está suficientemente lejos del objetivo, frena activamente
        if (distance >= escapeRadius)
        {
            // Aplicamos fuerza de frenado contraria a la velocidad actual (igual que Arrive)
            steer.linear = -agent.Velocity / Time.deltaTime;
            steer.linear = Vector3.ClampMagnitude(steer.linear, agent.MaxAcceleration);
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
