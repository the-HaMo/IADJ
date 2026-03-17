using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face : SteeringBehaviour
{
    [Header("Objetivos")]
    public Agent target;
    
    [Header("Parámetros de Delegación")]
    public float timeToTarget = 0.1f;
    
    // Comportamiento básico en el que delegamos
    private Align alignBehavior;
    // Agente virtual para pasar la orientación deseada a Align
    private Agent virtualTarget;

    void Awake()
    {
        this.nameSteering = "Face";
        
        // 1. Añadimos el componente básico automáticamente
        alignBehavior = gameObject.AddComponent<Align>();
        alignBehavior.enabled = false; // Lo mantenemos apagado para controlarlo nosotros
        
        // 2. Creamos el objetivo virtual
        virtualTarget = Agent.CreateStaticVirtual(Vector3.zero, 1f, 1f, 0f, false);
    }

    void OnDestroy()
    {
        // Limpiamos el agente virtual al destruir el componente
        if (virtualTarget != null && virtualTarget.gameObject != null)
        {
            Destroy(virtualTarget.gameObject);
        }
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

        // 1. Lógica de FACE: Calcular HACIA DÓNDE mirar
        Vector3 direction = target.Position - agent.Position;
        
        // Si el objetivo está en nuestra misma posición, no hay cambio
        if (direction.sqrMagnitude < 0.001f)
        {
            return new Steering();
        }
        
        // Calculamos la orientación que nos pondría frente al objetivo
        float targetOrientation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // 2. DELEGACIÓN REAL: Pasamos el ángulo a Align
        // Configuramos el objetivo virtual
        virtualTarget.Orientation = targetOrientation;
        
        // Copiamos los parámetros del target real para que Align use sus radios de frenado
        virtualTarget.InteriorAngle = target.InteriorAngle;
        virtualTarget.ExteriorAngle = target.ExteriorAngle;
        
        // Configuramos y llamamos a Align
        alignBehavior.target = virtualTarget;
        alignBehavior.timeToTarget = timeToTarget;
        
        return alignBehavior.GetSteering(agent);
    }
}
