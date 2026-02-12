using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SteeringBehaviour para que los miembros de una formación sigan al líder.
/// Se utiliza durante las transiciones cuando se rompe la formación para ir a un destino.
/// Los agentes mantienen una distancia relativa al líder y avanzan en su dirección.
/// </summary>
public class LeaderFollowingBehavior : SteeringBehaviour
{
    [SerializeField] private float maxAcceleration = 5f;
    [SerializeField] private float desiredDistance = 3f;  // Distancia a mantener del líder
    [SerializeField] private float slowingDistance = 5f;   // Distancia a la que comienza a frenar
    // [SerializeField] private FormationMember formationMember; // DESHABILITADO: No se usa en el sistema actual

    private void Start()
    {
        this.nameSteering = "Leader Following";
        
        // if (formationMember == null)
        //     formationMember = GetComponent<FormationMember>();
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        // Solo aplicar si el agente tiene un líder
        // NOTA: FormationMember deshabilitado, este behavior no se usa actualmente
        // if (formationMember == null)
        // {
        //     steer.linear = Vector3.zero;
        //     steer.angular = 0f;
        //     return steer;
        // }

        // AgentNPC leader = formationMember.GetLeader();
        AgentNPC leader = null; // Placeholder - este behavior no está activo
        
        if (leader == null)
        {
            steer.linear = Vector3.zero;
            steer.angular = 0f;
            return steer;
        }

        // Calcular dirección hacia el líder
        Vector3 directionToLeader = leader.Position - agent.Position;
        float distanceToLeader = directionToLeader.magnitude;

        // Si está muy cerca del líder, mantener distancia
        if (distanceToLeader < desiredDistance)
        {
            steer.linear = Vector3.zero;
            steer.angular = 0f;
            return steer;
        }

        // Calcular velocidad objetivo: seguir al líder pero mantener distancia
        float targetSpeed = leader.Velocity.magnitude;

        // Frenar si se estaría acercando demasiado
        if (distanceToLeader < slowingDistance)
        {
            float slowingFactor = (distanceToLeader - desiredDistance) / (slowingDistance - desiredDistance);
            targetSpeed = Mathf.Min(targetSpeed, leader.Velocity.magnitude * slowingFactor);
        }

        // Calcular velocidad deseada hacia el líder
        Vector3 desiredVelocity = directionToLeader.normalized * targetSpeed;

        // Acelerar hacia la velocidad deseada
        steer.linear = (desiredVelocity - agent.Velocity).normalized * maxAcceleration;

        // Rotación: siempre mirando en la dirección del movimiento
        if (agent.Velocity.magnitude > 0.1f)
        {
            float targetOrientation = Mathf.Atan2(agent.Velocity.x, agent.Velocity.z) * Mathf.Rad2Deg;
            float orientationDifference = targetOrientation - agent.Orientation;

            // Normalizar la diferencia entre -180 y 180
            while (orientationDifference > 180) orientationDifference -= 360;
            while (orientationDifference < -180) orientationDifference += 360;

            steer.angular = orientationDifference * 0.5f;
        }
        else
        {
            steer.angular = 0f;
        }

        return steer;
    }

    /// <summary>
    /// Estable la distancia deseada al líder
    /// </summary>
    public void SetDesiredDistance(float distance)
    {
        desiredDistance = distance;
    }
}
