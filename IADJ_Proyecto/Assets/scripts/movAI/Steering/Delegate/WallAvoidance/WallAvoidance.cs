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

            // Encontrar la colisión
            if (Physics.Raycast(agent.Position + Vector3.up * 0.5f, whiskerDir.normalized, out RaycastHit hit, rayVector.magnitude))
            {
                // Si la distancia es casi 0, es el propio personaje o el suelo, lo ignoramos
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

        // if not collision: return steer (como en el pseudocódigo)
        if (!foundCollision) return steer;

        // CORREGIDO: Aplicar fuerza en dirección de la normal para alejarse de la pared.
        // Usar un "Seek" a un punto calculado puede hacer que el personaje acelere hacia la pared.
        // Aplicando la normal directamente, el personaje siempre intentará separarse de la pared.
        steer.linear = closestHit.normal * agent.MaxAcceleration;
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