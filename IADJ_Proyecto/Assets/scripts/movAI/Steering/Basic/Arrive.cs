using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrive : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour
    public Agent target;
    public float timeToTarget;
    private bool isArriving = false;
    
    void Start()
    {
        this.nameSteering = "Arrive";
        isArriving = true;
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

        if (isArriving)
        {
            Steering steer = new Steering();
            Vector3 direction = target.Position - agent.Position;
            float distance = direction.magnitude;

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
            steer.linear = (targetVelocity - agent.Velocity) / timeToTarget;

            if (steer.linear.magnitude > agent.MaxAcceleration)
            {
                steer.linear = steer.linear.normalized * agent.MaxAcceleration;
            }
            steer.angular = 0f;
            return steer;
            
        }
        else
        {
            return new Steering();
        }
    }
}