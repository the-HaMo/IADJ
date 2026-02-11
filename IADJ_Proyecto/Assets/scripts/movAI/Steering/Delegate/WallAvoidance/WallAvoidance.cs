using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallAvoidance : SteeringBehaviour
{
    [Header("Wall Avoidance Parameters")]
    public float lookahead = 5f;
    public float avoidDistance = 2f;
    public int numWhiskers; // El número de bigotes se da en el inspector
    public float whiskerAngle = 30f;

    void Awake()
    {
        this.nameSteering = "WallAvoidance";
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        // 1. Calcular el objetivo para delegar a seek

        // rayVector = character.velocity
        Vector3 rayVector = agent.Velocity;
        
        // rayVector.normalized
        // (Si la velocidad es 0, usamos la orientación para poder detectar paredes al arrancar)
        if (rayVector.magnitude < 0.01f)
            rayVector = agent.OrientationToVector(agent.Orientation);
        rayVector.Normalize();

        // rayVector *= lookAhead
        rayVector *= lookahead;

        // Procesamos los n bigotes (collision detection)
        RaycastHit closestHit = new RaycastHit();
        bool foundCollision = false;
        float minDistance = float.MaxValue;

        for (int i = 0; i < numWhiskers; i++)
        {
            float angle = 0;
            if (numWhiskers > 1)
            {
                angle = (i - (numWhiskers - 1) / 2.0f) * whiskerAngle;
            }
            
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 whiskerDir = rotation * rayVector;

            // Encontrar la colisión (equivalente a collisionDetector.getCollision)
            // Se detecta todo sin máscara de capas
            if (Physics.Raycast(agent.Position, whiskerDir.normalized, out RaycastHit hit, rayVector.magnitude))
            {
                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    closestHit = hit;
                    foundCollision = true;
                }
            }
        }

        // if not collision: return steer (como en el pseudocódigo)
        if (!foundCollision) return steer;

        // target = collision.position + collision.normal * avoidDistance
        Vector3 targetPosition = closestHit.point + closestHit.normal * avoidDistance;

        // 2. Delegar a seek (calculamos aceleración lineal máxima hacia el target)
        Vector3 directionToTarget = targetPosition - agent.Position;
        
        // Al combinar con Wander, necesitamos que la aceleración sea máxima para "vencer"
        // el movimiento aleatorio del Wander mientras estemos en peligro de choque.
        steer.linear = directionToTarget.normalized * agent.MaxAcceleration;
        steer.angular = 0f;

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
            Gizmos.DrawLine(agent.Position, agent.Position + whiskerDir * lookahead);
        }
    }
}
