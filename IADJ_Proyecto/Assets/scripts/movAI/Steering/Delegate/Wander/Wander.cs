using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wander : SteeringBehaviour
{
    [Header("Parámetros del Círculo")]
    public float wanderOffset = 3f;
    public float wanderRadius = 2f;
    public float wanderRate = 5f; // Máximo cambio de orientación por frame

    [Header("Debug")]
    public bool showGizmos = true;
    
    // Comportamientos delegados
    private Face faceBehavior;
    private Agent virtualTarget;

    // Orientación actual del objetivo dentro del círculo de wander.
    private float wanderOrientation;

    void Awake()
    {
        this.nameSteering = "Wander";
        
        // 1. Añadimos el componente de Face (que ya delega en Align)
        faceBehavior = gameObject.AddComponent<Face>();
        faceBehavior.enabled = false;
        
        // 2. Creamos el objetivo virtual
        virtualTarget = Agent.CreateStaticVirtual(Vector3.zero, 1f, 1f, 0f, false);
    }

    void OnDestroy()
    {
        if (virtualTarget != null && virtualTarget.gameObject != null)
        {
            Destroy(virtualTarget.gameObject);
        }
    }

    private float RandomBinomial()
    {
        return Random.Range(-1f, 1f);
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        // 1. ACTUALIZAR EL CÍRCULO: Calcular el punto aleatorio
        
        // Actualizar la orientación del wander aleatoriamente
        wanderOrientation += RandomBinomial() * wanderRate;
        
        // Calcular la orientación objetivo relativa al agente
        float targetOrientation = wanderOrientation + agent.Orientation;
        
        // Calcular el centro del círculo de wander frente al agente
        Vector3 circleCenter = agent.Position + wanderOffset * agent.OrientationToVector(agent.Orientation);
        
        // Calcular la posición exacta del punto sobre el borde del círculo
        Vector3 targetPosition = circleCenter + wanderRadius * agent.OrientationToVector(targetOrientation);

        // 2. DELEGACIÓN REAL: Usar Face para encarar el punto
        virtualTarget.Position = targetPosition;
        faceBehavior.target = virtualTarget;
        
        // Obtenemos el steering angular de Face
        Steering steer = faceBehavior.GetSteering(agent);
        
        // 3. MOVIMIENTO LINEAL: Aceleración máxima hacia adelante
        // Siempre nos movemos en la dirección en la que estamos mirando actualmente
        steer.linear = agent.MaxAcceleration * agent.OrientationToVector(agent.Orientation);

        // Seguridad: Control de NaN
        if (float.IsNaN(steer.linear.x) || float.IsNaN(steer.linear.z))
            return new Steering();

        return steer;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Agent agent = GetComponent<Agent>();
        if (agent == null) return;

        Vector3 center = agent.Position + wanderOffset * agent.OrientationToVector(agent.Orientation);
        float targetOrientation = wanderOrientation + agent.Orientation;
        Vector3 target = center + wanderRadius * agent.OrientationToVector(targetOrientation);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, wanderRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(target, 0.3f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(agent.Position, target);
        
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(agent.Position, center);
    }
}