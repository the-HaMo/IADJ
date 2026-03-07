using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face : SteeringBehaviour
{
    // Overrides the Align.target member
    public Agent target;
    public float timeToTarget;
    
    void Awake()
    {
        this.nameSteering = "Face";
    }

    public override void NewTarget(Agent t) {
        target = t;
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        if (target == null)
        {
            return new Steering();
        }

        float targetRotation = 0f;
        Steering steer = new Steering();
        
        // 1. Calcular el objetivo para delegar a align
        
        // Calcular la dirección al objetivo
        Vector3 direction = target.Position - agent.Position;
        
        // Comprobar dirección cero
        if (direction.magnitude == 0){
            steer.linear = Vector3.zero;
            steer.angular = 0f;
            return steer;
        }
        
        // Calcular la orientación objetivo
        float targetOrientation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // 2. Delegar a align
        float rotation = targetOrientation - agent.Orientation;
        rotation = Bodi.MapToRange(rotation, Range.Degrees);

        float rotationSize = Mathf.Abs(rotation);

        if (rotationSize < target.InteriorAngle) {
            steer.linear = Vector3.zero;
            steer.angular = 0f;
            return steer;
        }

        if (rotationSize > target.ExteriorAngle){
            targetRotation = agent.MaxRotation;
        }
        else {
            targetRotation = agent.MaxRotation * rotationSize / target.ExteriorAngle;
        }

        targetRotation *= rotation / rotationSize;

        steer.angular = (targetRotation - agent.Rotation) / timeToTarget;

        if (Mathf.Abs(steer.angular) > agent.MaxAngularAcc) {
            steer.angular /= Mathf.Abs(steer.angular);
            steer.angular *= agent.MaxAngularAcc;
        }

        steer.linear = Vector3.zero;
        return steer;
    }
}
