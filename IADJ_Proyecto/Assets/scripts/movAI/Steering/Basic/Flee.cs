using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flee : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour

    public Agent target;
    
    void Start()
    {
        this.nameSteering = "Flee";
    }
    
    public override void NewTarget(Agent t)
    {
        this.target = t;
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        // Calcula el steering.
        Vector3 desiredVelocity = (agent.Position - target.Position).normalized * agent.MaxSpeed;
        steer.linear = desiredVelocity - agent.Velocity;
        steer.angular = 0f; 
        // Retornamos el resultado final.
        return steer;
    }
}