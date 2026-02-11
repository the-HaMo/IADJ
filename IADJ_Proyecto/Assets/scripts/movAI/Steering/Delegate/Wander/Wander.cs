using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wander : SteeringBehaviour
{
    // Radio y distancia del círculo de wander.
    public float wanderOffset;
    public float wanderRadius;
    
    // Máximo cambio de orientación del wander por frame.
    public float wanderRate;
    
    // Orientación actual del objetivo de wander.
    private float wanderOrientation;

    void Awake(){
        this.nameSteering = "Wander";
    }

    private float RandomBinomial(){
        return Random.Range(-1f, 1f);
    }

    public override Steering GetSteering(AgentNPC agent){
        Steering steering = new Steering();
        
        // 1. Calcular el objetivo para delegar a face
        
        // Actualizar la orientación del wander
        wanderOrientation += RandomBinomial() * wanderRate;
        
        // Calcular la orientación objetivo combinada
        float targetOrientation = wanderOrientation + agent.Orientation;
        
        // Calcular el centro del círculo de wander
        Vector3 target = agent.Position + wanderOffset * agent.OrientationToVector(agent.Orientation);
        
        // Calcular la posición del objetivo
        target += wanderRadius * agent.OrientationToVector(targetOrientation);
        
        // 2. Delegar a face
        Vector3 direction = target - agent.Position;
        float orientacionObj = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        
        float rotation = orientacionObj - agent.Orientation;
        rotation = Bodi.MapToRange(rotation, Range.Degrees);
        float rotationSize = Mathf.Abs(rotation);
        
        float targetRotationSpeed = 0f;
        if (rotationSize > 1f){
            targetRotationSpeed = agent.MaxRotation;
            targetRotationSpeed *= rotation / rotationSize;
        }
        
        float angularAccel = (targetRotationSpeed - agent.Rotation) / 0.1f;
        if (Mathf.Abs(angularAccel) > agent.MaxAngularAcc){
            angularAccel = Mathf.Sign(angularAccel) * agent.MaxAngularAcc;
        }
        
        steering.angular = angularAccel;
        
        // 3. Aceleración lineal a tope en la dirección de la orientación
        steering.linear = agent.MaxAcceleration * agent.OrientationToVector(agent.Orientation);
        
        // Devolver el steering
        return steering;
    }
}