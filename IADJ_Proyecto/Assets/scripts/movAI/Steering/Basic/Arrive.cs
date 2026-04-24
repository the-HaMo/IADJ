using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrive : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour
    public Agent target;
    public float timeToTarget;
    void Awake()
    {
        this.nameSteering = "Arrive";
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
        Vector3 direction = target.Position - agent.Position;
        float distance = direction.magnitude;

        // Evitar división por cero si ya estamos en el destino
        if (distance < 0.001f)
        {
            return new Steering();
        }

        if (distance < target.InteriorRadius)
        {
            steer.linear = -agent.Velocity / Time.deltaTime; 
            steer.linear = Vector3.ClampMagnitude(steer.linear, agent.MaxAcceleration);
            return steer;
        }
        
        float targetSpeed;
        if (distance > target.ArrivalRadius)
        {
            targetSpeed = agent.MaxSpeed;
        }
        else
        {
            targetSpeed = agent.MaxSpeed * distance / target.ArrivalRadius;
        }
        Vector3 targetVelocity = direction.normalized * targetSpeed;
        
        // Protección contra división por cero en timeToTarget
        float safeTimeToTarget = timeToTarget > 0 ? timeToTarget : 0.1f;
        steer.linear = (targetVelocity - agent.Velocity) / safeTimeToTarget;

        if (steer.linear.magnitude > agent.MaxAcceleration)
        {
            steer.linear = steer.linear.normalized * agent.MaxAcceleration;
        }
        steer.angular = 0f;
        return steer;
    }
}