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

    void Awake() {
        this.nameSteering = "Interpose";
    }

    public override Steering GetSteering(AgentNPC agent) {
        if (targetA == null || targetB == null) return new Steering();

        // 1. Calcular punto medio actual
        Vector3 midPoint = (targetA.Position + targetB.Position) / 2.0f;

        // 2. Estimar tiempo para llegar allí
        float distance = Vector3.Distance(agent.Position, midPoint);
        float time = distance / agent.MaxSpeed;

        // 3. Predecir posiciones futuras basándonos en ese tiempo
        Vector3 futurePosA = targetA.Position + targetA.Velocity * time;
        Vector3 futurePosB = targetB.Position + targetB.Velocity * time;

        // 4. El objetivo real es el punto medio de las predicciones
        Vector3 targetPosition = (futurePosA + futurePosB) / 2.0f;

        // 5. Aplicar lógica de Arrive hacia ese punto
        return ArriveToPoint(agent, targetPosition);
    }

    private Steering ArriveToPoint(AgentNPC agent, Vector3 pos) {
        Steering steer = new Steering();
        Vector3 direction = pos - agent.Position;
        float dist = direction.magnitude;

        if (dist < interiorRadius) {
            // Frenazo si ya estamos en el sitio
            steer.linear = -agent.Velocity / Time.deltaTime;
        } else {
            float targetSpeed = (dist > arrivalRadius) ? agent.MaxSpeed : agent.MaxSpeed * dist / arrivalRadius;
            Vector3 targetVelocity = direction.normalized * targetSpeed;
            steer.linear = (targetVelocity - agent.Velocity) / timeToTarget;
        }
        
        steer.linear = Vector3.ClampMagnitude(steer.linear, agent.MaxAcceleration);
        return steer;
    }
}
