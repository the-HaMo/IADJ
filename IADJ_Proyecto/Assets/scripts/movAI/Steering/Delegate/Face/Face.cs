using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face : SteeringBehaviour
{
    // Overrides the Align.target member
    public Agent target;
    public float timeToTarget=0.1f;
    
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

        // Determinamos la velocidad de rotación deseada (targetRotation)
        if (rotationSize < target.InteriorAngle) {
            // Si estamos dentro del ángulo interior, queremos detenernos (velocidad 0)
            targetRotation = 0f;
        }
        else if (rotationSize > target.ExteriorAngle){
            // Si estamos fuera del ángulo exterior, rotamos a máxima velocidad
            targetRotation = agent.MaxRotation;
        }
        else {
            // Si estamos entre ambos, escalamos la velocidad proporcionalmente
            targetRotation = agent.MaxRotation * rotationSize / target.ExteriorAngle;
        }

        // Aplicamos la dirección (signo) a la velocidad deseada
        if (rotationSize > 0.001f) {
            targetRotation *= rotation / rotationSize;
        }

        // Calculamos la aceleración necesaria para alcanzar targetRotation en timeToTarget
        // Al ser targetRotation = 0 cuando estamos cerca, esto genera el frenado contra la inercia
        steer.angular = (targetRotation - agent.Rotation) / timeToTarget;

        if (Mathf.Abs(steer.angular) > agent.MaxAngularAcc) {
            steer.angular = Mathf.Sign(steer.angular) * agent.MaxAngularAcc;
        }

        steer.linear = Vector3.zero;
        return steer;
    }
}
