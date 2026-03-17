using UnityEngine;

public class Interpose : SteeringBehaviour
{
    [Header("Objetivos a interceptar")]
    public Agent targetA;
    public Agent targetB;

    [Header("Parámetros de Arrive (Delegado)")]
    public float timeToTarget = 0.1f;
    public float arrivalRadius = 2f;
    public float interiorRadius = 0.5f;

    // Comportamiento básico en el que delegamos
    private Arrive arriveBehavior;
    // Agente virtual para pasar la posición calculada a Arrive
    private Agent virtualTarget;

    void Awake()
    {
        this.nameSteering = "Interpose";
        
        // 1. Añadimos el componente básico automáticamente
        arriveBehavior = gameObject.AddComponent<Arrive>();
        arriveBehavior.enabled = false; // Lo mantenemos apagado
        
        // 2. Creamos el objetivo virtual
        virtualTarget = Agent.CreateStaticVirtual(Vector3.zero, interiorRadius, arrivalRadius, 0f, false);
    }

    void OnDestroy()
    {
        // Limpiamos el agente virtual
        if (virtualTarget != null && virtualTarget.gameObject != null)
        {
            Destroy(virtualTarget.gameObject);
        }
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        if (targetA == null || targetB == null) return new Steering();

        // 1. Lógica de INTERPOSE: Calcular punto de intercepción
        
        // Calcular punto medio actual
        Vector3 midPoint = (targetA.Position + targetB.Position) / 2.0f;

        // Estimar tiempo para llegar allí
        float distance = Vector3.Distance(agent.Position, midPoint);
        float time = distance / agent.MaxSpeed;

        // Predecir posiciones futuras basándonos en ese tiempo
        Vector3 futurePosA = targetA.Position + targetA.Velocity * time;
        Vector3 futurePosB = targetB.Position + targetB.Velocity * time;

        // El objetivo real es el punto medio de las predicciones
        Vector3 targetPosition = (futurePosA + futurePosB) / 2.0f;

        // 2. DELEGACIÓN REAL: Usar Arrive para ir allí
        virtualTarget.Position = targetPosition;
        virtualTarget.InteriorRadius = interiorRadius;
        virtualTarget.ArrivalRadius = arrivalRadius;
        
        arriveBehavior.target = virtualTarget;
        arriveBehavior.timeToTarget = timeToTarget;

        return arriveBehavior.GetSteering(agent);
    }
}
