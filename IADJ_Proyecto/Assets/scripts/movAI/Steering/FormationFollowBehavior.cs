using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SteeringBehaviour para mantener un agente en su posición asignada dentro de una formación.
/// Cuando está en formación, el agente busca alcanzar su posición específica.
/// El agente siempre avanza hacia adelante según su dirección de velocidad.
/// </summary>
public class FormationFollowBehavior : SteeringBehaviour
{
    [SerializeField] private float maxAcceleration = 5f;
    [SerializeField] private float slowingRadius = 3f;
    // [SerializeField] private FormationMember formationMember; // DESHABILITADO: No se usa en el sistema actual

    private void Start()
    {
        this.nameSteering = "Formation Follow";
        
        // if (formationMember == null)
        //     formationMember = GetComponent<FormationMember>();
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        // Solo aplicar este steering si el agente está en formación
        // NOTA: FormationMember deshabilitado, este behavior no se usa actualmente
        // if (formationMember == null || !formationMember.IsInFormation())
        // {
        //     steer.linear = Vector3.zero;
        //     steer.angular = 0f;
        //     return steer;
        // }

        // Obtener la posición objetivo en la formación
        // Vector3 targetPosition = formationMember.GetFormationTargetPosition();
        Vector3 targetPosition = agent.Position; // Placeholder - este behavior no está activo
        Vector3 currentPosition = agent.Position;
        Vector3 direction = targetPosition - currentPosition;
        float distance = direction.magnitude;

        // Si está muy cerca, mantener posición
        if (distance < agent.InteriorRadius)
        {
            steer.linear = Vector3.zero;
            steer.angular = 0f;
            return steer;
        }

        // Calcular velocidad: se frena cuando se acerca al destino
        float targetSpeed = agent.MaxSpeed;

        if (distance < slowingRadius)
        {
            targetSpeed = agent.MaxSpeed * (distance / slowingRadius);
        }

        // Dirección normalizada hacia el objetivo
        Vector3 desiredVelocity = direction.normalized * targetSpeed;

        // Calcular aceleración necesaria
        steer.linear = (desiredVelocity - agent.Velocity).normalized * maxAcceleration;

        // Rotación: el agente siempre mira en la dirección de su movimiento
        if (agent.Velocity.magnitude > 0.1f)
        {
            float targetOrientation = Mathf.Atan2(agent.Velocity.x, agent.Velocity.z) * Mathf.Rad2Deg;
            float orientationDifference = targetOrientation - agent.Orientation;

            // Normalizar la diferencia entre -180 y 180
            while (orientationDifference > 180) orientationDifference -= 360;
            while (orientationDifference < -180) orientationDifference += 360;

            steer.angular = orientationDifference * 0.5f; // Factor de suavidad
        }
        else
        {
            steer.angular = 0f;
        }

        return steer;
    }
}
