using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallAvoidance : SteeringBehaviour
{
    [Header("Wall Avoidance Parameters")]
    public float lookahead = 5f;
    public float avoidDistance = 2f;
    public int numWhiskers = 3; 
    public float whiskerAngle = 30f;

    // Comportamientos delegados
    private Seek seekBehavior;
    private Agent virtualTarget;

    void Awake()
    {
        this.nameSteering = "WallAvoidance";
        
        // 1. Añadimos el componente de Seek automáticamente
        seekBehavior = gameObject.AddComponent<Seek>();
        seekBehavior.enabled = false;
        
        // 2. Creamos el objetivo virtual
        virtualTarget = Agent.CreateStaticVirtual(Vector3.zero, 0.5f, 1f, 0f, false);
    }

    void OnDestroy()
    {
        if (virtualTarget != null && virtualTarget.gameObject != null)
        {
            Destroy(virtualTarget.gameObject);
        }
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        // 1. DETECCIÓN DE COLISIÓN (Raycasting)
        Vector3 rayVector = agent.Velocity;
        if (rayVector.magnitude < 0.01f)
            rayVector = agent.OrientationToVector(agent.Orientation);
        
        rayVector.Normalize();
        rayVector *= lookahead;

        RaycastHit closestHit = new RaycastHit();
        bool foundCollision = false;
        float minDistance = float.MaxValue;

        // Procesamos los bigotes
        for (int i = 0; i < numWhiskers; i++)
        {
            float angle = 0;
            if (numWhiskers > 1)
                angle = (i - (numWhiskers - 1) / 2.0f) * whiskerAngle;
            
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 whiskerDir = rotation * rayVector;

            // Lanzamos el rayo desde el centro del agente para evitar el suelo
            if (Physics.Raycast(agent.Position + Vector3.up * 0.5f, whiskerDir.normalized, out RaycastHit hit, rayVector.magnitude))
            {
                if (hit.collider.gameObject == agent.gameObject || hit.distance < 0.2f)
                    continue;

                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    closestHit = hit;
                    foundCollision = true;
                }
            }
        }

        if (!foundCollision) return new Steering();

        // 2. DELEGACIÓN REAL OPTIMIZADA
        
        // TRUCO: El punto objetivo NO es el punto de impacto, sino "Mi posición + la Normal".
        // Esto hace que Seek genere un vector que apunta exactamente hacia donde nos empuja la pared.
        Vector3 targetPosition = agent.Position + closestHit.normal * avoidDistance;
        
        virtualTarget.Position = targetPosition;
        seekBehavior.Target = virtualTarget;
        
        // Obtenemos la dirección delegando en Seek
        Steering steer = seekBehavior.GetSteering(agent);
        
        // IMPORTANTE: Para paredes, usamos Aceleración Máxima para que sea una maniobra de urgencia
        // Si usamos solo MaxSpeed (como hace el Seek básico), a veces no gira a tiempo.
        steer.linear = steer.linear.normalized * agent.MaxAcceleration;
        
        return steer;
    }

    private void OnDrawGizmosSelected()
    {
        Agent agent = GetComponent<Agent>();
        if (agent == null) return;

        Vector3 direction = agent.Velocity.normalized;
        if (direction.magnitude < 0.01f)
            direction = agent.OrientationToVector(agent.Orientation);

        Gizmos.color = Color.red;
        for (int i = 0; i < numWhiskers; i++)
        {
            float angle = 0;
            if (numWhiskers > 1)
                angle = (i - (numWhiskers - 1) / 2.0f) * whiskerAngle;
            
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 whiskerDir = rotation * direction;
            Gizmos.DrawLine(agent.Position + Vector3.up * 0.5f, agent.Position + Vector3.up * 0.5f + whiskerDir * lookahead);
        }
    }
}