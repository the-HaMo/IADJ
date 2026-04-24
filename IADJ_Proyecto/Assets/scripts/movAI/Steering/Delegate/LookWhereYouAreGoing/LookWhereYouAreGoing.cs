using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookWhereYouAreGoing : SteeringBehaviour
{
    [Header("Delegación")]
    public float timeToTarget = 0.1f;
    
    private Align alignBehavior;
    private Agent virtualTarget;

    void Awake()
    {
        this.nameSteering = "LookWhereYouAreGoing";
        
        // Creamos el Align en el que delegaremos la rotación
        alignBehavior = gameObject.AddComponent<Align>();
        alignBehavior.enabled = false;
        
        // Agente virtual para pasarle la orientación deseada a Align
        virtualTarget = Agent.CreateStaticVirtual(Vector3.zero, 1f, 1f, 0f, false);
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
        // Si no nos estamos moviendo, no hay rotación nueva
        if (agent.Velocity.sqrMagnitude < 0.001f)
        {
            return new Steering();
        }

        // Calculamos la orientación basada en el vector de velocidad actual
        float targetOrientation = Mathf.Atan2(agent.Velocity.x, agent.Velocity.z) * Mathf.Rad2Deg;

        // Configuramos el objetivo virtual
        virtualTarget.Orientation = targetOrientation;
        
        // Delegamos en Align
        alignBehavior.target = virtualTarget;
        alignBehavior.timeToTarget = timeToTarget;
        
        return alignBehavior.GetSteering(agent);
    }
}
