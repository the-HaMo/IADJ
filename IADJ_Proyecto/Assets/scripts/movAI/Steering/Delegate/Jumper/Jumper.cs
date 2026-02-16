using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jumper : SteeringBehaviour
{
    [System.Serializable]
    public class JumpTrigger
    {
        [Tooltip("Punto desde donde se debe saltar")]
        public Transform jumpPoint;
        
        [Tooltip("Punto donde se debe aterrizar")]
        public Transform landingPoint;
        
        [Tooltip("Radio de detección para activar el salto")]
        public float triggerRadius = 2f;
        
        [Tooltip("Velocidad vertical específica para este salto (0 = usar default)")]
        public float customJumpSpeed = 0f;
        
        [HideInInspector]
        public bool hasBeenUsed = false;
    }

    [Header("Jump Points Configuration")]
    [Tooltip("Lista de puntos de salto en el mundo")]
    public List<JumpTrigger> jumpTriggers = new List<JumpTrigger>();
    
    [Header("Jump Physics")]
    [Tooltip("Velocidad vertical máxima del salto por defecto")]
    public float maxYVelocity = 10f;
    
    [Tooltip("Velocidad máxima horizontal del personaje")]
    public float maxSpeed = 5f;
    
    [Tooltip("Gravedad personalizada (0 = usar Physics.gravity)")]
    public float customGravity = 0f;
    
    [Header("Jump Execution")]
    [Tooltip("Modo de ejecución del salto")]
    public JumpMode jumpMode = JumpMode.DirectVelocity;
    
    [Tooltip("Margen de error para considerar que hemos aterrizado")]
    public float landingTolerance = 1.5f;
    
    [Tooltip("Altura mínima sobre el suelo para considerar que estamos en el aire")]
    public float airborneHeight = 0.5f;

    [Header("Reset Options")]
    [Tooltip("Resetear saltos usados al completar todos")]
    public bool autoResetOnComplete = true;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    public bool showDebugLogs = true;
    public Color jumpPointColor = Color.yellow;
    public Color landingPointColor = Color.green;
    public Color activeJumpColor = Color.red;

    public enum JumpMode
    {
        DirectVelocity,      // Modifica directamente la velocidad del agente (recomendado)
        DirectPosition,      // Modifica directamente la posición (más forzado)
        SteeringOnly         // Solo usa steering (puede no funcionar con gravedad)
    }

    // Estado interno
    private JumpTrigger activeJump = null;
    private Vector3 calculatedVelocity;
    private Vector3 currentVelocity; // Velocidad que controlamos nosotros
    private bool isCalculated = false;
    private bool isExecutingJump = false;
    private bool impulseApplied = false;
    private bool hasLanded = false;
    private float jumpStartTime = 0f;
    private Vector3 jumpStartPosition;
    private float timeInAir = 0f;

    void Awake()
    {
        this.nameSteering = "Jumper";
    }

    void Update()
    {
        // Si estamos en modo DirectPosition, actualizar posición aquí
        if (isExecutingJump && jumpMode == JumpMode.DirectPosition)
        {
            UpdateJumpPosition(GetComponent<AgentNPC>());
        }
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        // Si no hay triggers configurados, no hacer nada
        if (jumpTriggers == null || jumpTriggers.Count == 0)
        {
            return steer;
        }

        // **FASE 1: Detectar si estamos cerca de un punto de salto**
        if (!isExecutingJump)
        {
            activeJump = DetectNearestJumpTrigger(agent);
            
            if (activeJump != null)
            {
                // Calcular la velocidad necesaria para este salto
                float jumpVel = activeJump.customJumpSpeed > 0 ? activeJump.customJumpSpeed : maxYVelocity;
                calculatedVelocity = CalculateJumpVelocity(
                    agent.Position,
                    activeJump.landingPoint.position,
                    jumpVel
                );
                
                isCalculated = true;
                isExecutingJump = true;
                impulseApplied = false;
                hasLanded = false;
                jumpStartTime = Time.time;
                timeInAir = 0f;
                jumpStartPosition = agent.Position;
                currentVelocity = calculatedVelocity;
                
                // Marcar como usado
                activeJump.hasBeenUsed = true;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[Jump] ========== INICIO DE SALTO ==========");
                    Debug.Log($"[Jump] Desde: {activeJump.jumpPoint.name} → {activeJump.landingPoint.name}");
                    Debug.Log($"[Jump] Posición inicial: {agent.Position}");
                    Debug.Log($"[Jump] Velocidad calculada: {calculatedVelocity}");
                    Debug.Log($"[Jump] Modo: {jumpMode}");
                }
            }
        }

        // **FASE 2: Ejecutar el salto**
        if (isExecutingJump && activeJump != null)
        {
            timeInAir += Time.deltaTime;

            // Verificar si ya hemos aterrizado
            if (CheckIfLanded(agent, activeJump.landingPoint.position))
            {
                hasLanded = true;
                isExecutingJump = false;
                isCalculated = false;
                impulseApplied = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[Jump] ========== ATERRIZAJE ==========");
                    Debug.Log($"[Jump] Posición final: {agent.Position}");
                    Debug.Log($"[Jump] Tiempo en aire: {timeInAir:F2}s");
                }
                
                // Verificar si completamos todos los saltos
                if (autoResetOnComplete && AllJumpsCompleted())
                {
                    ResetAllJumps();
                    if (showDebugLogs)
                    {
                        Debug.Log("[Jump] Todos los saltos completados. Reseteando...");
                    }
                }
                
                activeJump = null;
                return steer;
            }

            // **APLICAR SALTO SEGÚN EL MODO**
            switch (jumpMode)
            {
                case JumpMode.DirectVelocity:
                    return ApplyDirectVelocityJump(agent);
                
                case JumpMode.DirectPosition:
                    // La posición se actualiza en Update()
                    return steer;
                
                case JumpMode.SteeringOnly:
                    return ApplySteeringJump(agent);
            }
        }

        return steer;
    }

    // MODO 1: Modificar velocidad directamente (recomendado)
    private Steering ApplyDirectVelocityJump(AgentNPC agent)
    {
        Steering steer = new Steering();

        if (!impulseApplied)
        {
            // ACTIVAR MOVIMIENTO VERTICAL
            agent.allowVerticalMovement = true;
            
            // Aplicar impulso inicial
            agent.Velocity = new Vector3(calculatedVelocity.x, calculatedVelocity.y, calculatedVelocity.z);
            impulseApplied = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[Jump] Impulso aplicado - Velocidad ahora: {agent.Velocity}");
                Debug.Log($"[Jump] allowVerticalMovement = true");
            }
        }
        else
        {
            // Durante el vuelo: la gravedad se aplica en AgentNPC.ApplySteering()
            
            // Pequeñas correcciones horizontales hacia el landing
            Vector3 toLanding = activeJump.landingPoint.position - agent.Position;
            toLanding.y = 0;
            
            if (toLanding.magnitude > 0.1f)
            {
                toLanding.Normalize();
                Vector3 desiredHorizontal = toLanding * maxSpeed;
                Vector3 currentHorizontal = new Vector3(agent.Velocity.x, 0, agent.Velocity.z);
                
                Vector3 correction = (desiredHorizontal - currentHorizontal) * 0.3f;
                
                steer.linear = correction / Time.deltaTime;
                
                if (steer.linear.magnitude > agent.MaxAcceleration * 0.3f)
                {
                    steer.linear = steer.linear.normalized * agent.MaxAcceleration * 0.3f;
                }
            }
            
            if (showDebugLogs && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[Jump] En vuelo - Y: {agent.Position.y:F2} Vel.y: {agent.Velocity.y:F2} AllowVertical: {agent.allowVerticalMovement}");
            }
        }

        return steer;
    }

    // MODO 2: Modificar posición directamente (muy forzado pero garantizado)
    private void UpdateJumpPosition(AgentNPC agent)
    {
        if (!isExecutingJump || activeJump == null) return;

        float gravity = customGravity > 0 ? customGravity : Mathf.Abs(Physics.gravity.y);
        float t = timeInAir;

        // Calcular nueva posición usando ecuaciones de movimiento
        Vector3 newPosition = jumpStartPosition + new Vector3(
            calculatedVelocity.x * t,
            calculatedVelocity.y * t - 0.5f * gravity * t * t,
            calculatedVelocity.z * t
        );

        // Aplicar directamente
        agent.Position = newPosition;

        // Calcular velocidad para que coincida
        agent.Velocity = new Vector3(
            calculatedVelocity.x,
            calculatedVelocity.y - gravity * t,
            calculatedVelocity.z
        );

        if (showDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[Jump] DirectPos - Y: {newPosition.y:F2} Vel.y: {agent.Velocity.y:F2} t: {t:F2}");
        }
    }

    // MODO 3: Solo steering (puede no funcionar si hay gravedad externa)
    private Steering ApplySteeringJump(AgentNPC agent)
    {
        Steering steer = new Steering();

        if (!impulseApplied)
        {
            Vector3 deltaVelocity = calculatedVelocity - agent.Velocity;
            steer.linear = deltaVelocity / Time.fixedDeltaTime;
            impulseApplied = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[Jump] Steering aplicado: {steer.linear}");
            }
        }
        else
        {
            // Mantener trayectoria
            Vector3 toLanding = activeJump.landingPoint.position - agent.Position;
            toLanding.y = 0;
            
            if (toLanding.magnitude > 0.1f)
            {
                toLanding.Normalize();
                Vector3 desiredVel = toLanding * maxSpeed;
                Vector3 currentHorizontal = new Vector3(agent.Velocity.x, 0, agent.Velocity.z);
                
                steer.linear = (desiredVel - currentHorizontal) / Time.fixedDeltaTime;
                
                if (steer.linear.magnitude > agent.MaxAcceleration * 0.3f)
                {
                    steer.linear = steer.linear.normalized * agent.MaxAcceleration * 0.3f;
                }
            }
        }

        return steer;
    }

    private JumpTrigger DetectNearestJumpTrigger(AgentNPC agent)
    {
        JumpTrigger nearest = null;
        float minDistance = float.MaxValue;

        foreach (var trigger in jumpTriggers)
        {
            if (trigger.hasBeenUsed || trigger.jumpPoint == null || trigger.landingPoint == null)
                continue;

            float distance = Vector3.Distance(agent.Position, trigger.jumpPoint.position);
            
            if (distance <= trigger.triggerRadius && distance < minDistance)
            {
                minDistance = distance;
                nearest = trigger;
            }
        }

        return nearest;
    }

    private Vector3 CalculateJumpVelocity(Vector3 startPos, Vector3 endPos, float jumpVelocity)
    {
        Vector3 deltaPos = endPos - startPos;
        float gravity = customGravity > 0 ? customGravity : Mathf.Abs(Physics.gravity.y);
        if (gravity < 0.1f) gravity = 9.81f;
        
        float deltaY = deltaPos.y;
        float deltaX = deltaPos.x;
        float deltaZ = deltaPos.z;
        
        float a = -0.5f * gravity;
        float b = jumpVelocity;
        float c = -deltaY;
        
        float discriminant = b * b - 4 * a * c;
        
        if (discriminant < 0)
        {
            float minJumpVel = Mathf.Sqrt(2 * gravity * Mathf.Abs(deltaY));
            jumpVelocity = minJumpVel * 1.3f;
            b = jumpVelocity;
            discriminant = b * b - 4 * a * c;
            
            if (showDebugLogs)
            {
                Debug.LogWarning($"[Jump] Velocidad ajustada a {jumpVelocity:F2} para alcanzar altura");
            }
        }
        
        float sqrtDisc = Mathf.Sqrt(Mathf.Abs(discriminant));
        float time1 = (-b + sqrtDisc) / (2 * a);
        float time2 = (-b - sqrtDisc) / (2 * a);
        
        float time = 0;
        if (time1 > 0 && time2 > 0)
            time = Mathf.Min(time1, time2);
        else if (time1 > 0)
            time = time1;
        else if (time2 > 0)
            time = time2;
        else
            time = Mathf.Sqrt(2 * Mathf.Abs(deltaY) / gravity);
        
        time = Mathf.Max(time, 0.1f);
        
        float vx = deltaX / time;
        float vz = deltaZ / time;
        
        float horizontalSpeed = Mathf.Sqrt(vx * vx + vz * vz);
        
        if (horizontalSpeed > maxSpeed)
        {
            float scale = maxSpeed / horizontalSpeed;
            vx *= scale;
            vz *= scale;
            
            if (showDebugLogs)
            {
                Debug.LogWarning($"[Jump] Velocidad horizontal limitada de {horizontalSpeed:F2} a {maxSpeed:F2}");
            }
        }
        
        return new Vector3(vx, jumpVelocity, vz);
    }

    private bool CheckIfLanded(AgentNPC agent, Vector3 landingPos)
    {
        Vector3 agentPosFlat = new Vector3(agent.Position.x, 0, agent.Position.z);
        Vector3 landingPosFlat = new Vector3(landingPos.x, 0, landingPos.z);
        float horizontalDist = Vector3.Distance(agentPosFlat, landingPosFlat);
        
        float verticalDist = Mathf.Abs(agent.Position.y - landingPos.y);
        
        bool isLow = agent.Position.y <= landingPos.y + airborneHeight;
        bool enoughTimeAirborne = timeInAir > 0.3f;
        bool closeHorizontally = horizontalDist < landingTolerance;
        bool closeVertically = verticalDist < landingTolerance;
        
        bool landed = closeHorizontally && closeVertically && isLow && enoughTimeAirborne;
        
        if (landed)
        {
            // DESACTIVAR MOVIMIENTO VERTICAL al aterrizar
            agent.allowVerticalMovement = false;
            
            if (showDebugLogs)
            {
                Debug.Log($"[Jump] ATERRIZAJE DETECTADO - allowVerticalMovement = false");
            }
        }
        
        if (showDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[Jump] Landing check - HDist:{horizontalDist:F2} VDist:{verticalDist:F2} Low:{isLow} Time:{timeInAir:F2}");
        }
        
        return landed;
    }

    private bool AllJumpsCompleted()
    {
        foreach (var trigger in jumpTriggers)
        {
            if (!trigger.hasBeenUsed)
                return false;
        }
        return true;
    }

    public void ResetAllJumps()
    {
        foreach (var trigger in jumpTriggers)
        {
            trigger.hasBeenUsed = false;
        }
        
        activeJump = null;
        isExecutingJump = false;
        isCalculated = false;
        impulseApplied = false;
        hasLanded = false;
    }

    public void ResetJump(int index)
    {
        if (index >= 0 && index < jumpTriggers.Count)
        {
            jumpTriggers[index].hasBeenUsed = false;
        }
    }

    public bool IsJumping()
    {
        return isExecutingJump;
    }

    public bool HasLanded()
    {
        return hasLanded;
    }

    public JumpTrigger GetActiveJump()
    {
        return activeJump;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || jumpTriggers == null) return;

        foreach (var trigger in jumpTriggers)
        {
            if (trigger.jumpPoint == null || trigger.landingPoint == null)
                continue;

            Color color = trigger.hasBeenUsed ? Color.gray : 
                         (trigger == activeJump ? activeJumpColor : jumpPointColor);

            Gizmos.color = color;
            Gizmos.DrawWireSphere(trigger.jumpPoint.position, 0.5f);
            
            Gizmos.color = color * 0.3f;
            Gizmos.DrawWireSphere(trigger.jumpPoint.position, trigger.triggerRadius);

            Gizmos.color = trigger.hasBeenUsed ? Color.gray : landingPointColor;
            Gizmos.DrawWireSphere(trigger.landingPoint.position, 0.5f);
            
            Gizmos.color = (trigger.hasBeenUsed ? Color.gray : landingPointColor) * 0.3f;
            Gizmos.DrawWireSphere(trigger.landingPoint.position, landingTolerance);

            DrawPredictedJumpArc(
                trigger.jumpPoint.position, 
                trigger.landingPoint.position,
                trigger.customJumpSpeed > 0 ? trigger.customJumpSpeed : maxYVelocity,
                color
            );

            #if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            
            UnityEditor.Handles.Label(
                trigger.jumpPoint.position + Vector3.up * 1.5f,
                $"JUMP {jumpTriggers.IndexOf(trigger)}\nR: {trigger.triggerRadius:F1}",
                style
            );
            
            style.normal.textColor = landingPointColor;
            UnityEditor.Handles.Label(
                trigger.landingPoint.position + Vector3.up * 1.5f,
                $"LAND {jumpTriggers.IndexOf(trigger)}",
                style
            );
            #endif
        }

        if (isExecutingJump && activeJump != null)
        {
            Agent agent = GetComponent<Agent>();
            if (agent != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(agent.Position, activeJump.landingPoint.position);
                Gizmos.DrawWireSphere(agent.Position, 0.3f);
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(agent.Position, agent.Velocity);
                
                // Mostrar tiempo en aire
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    agent.Position + Vector3.up * 2f,
                    $"Airborne: {timeInAir:F2}s\nY: {agent.Position.y:F2}\nVel.y: {agent.Velocity.y:F2}",
                    new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } }
                );
                #endif
            }
        }
    }

    private void DrawPredictedJumpArc(Vector3 start, Vector3 end, float jumpVel, Color color)
    {
        Gizmos.color = color;
        
        Vector3 velocity = CalculateJumpVelocity(start, end, jumpVel);
        float gravity = customGravity > 0 ? customGravity : Mathf.Abs(Physics.gravity.y);
        if (gravity < 0.1f) gravity = 9.81f;
        
        int segments = 25;
        Vector3 lastPoint = start;
        float dt = 0.05f;
        
        for (int i = 1; i <= segments; i++)
        {
            float t = i * dt;
            
            Vector3 point = start + new Vector3(
                velocity.x * t,
                velocity.y * t - 0.5f * gravity * t * t,
                velocity.z * t
            );
            
            Gizmos.DrawLine(lastPoint, point);
            
            if (point.y < end.y - 1f)
                break;
            
            lastPoint = point;
        }
        
        Gizmos.color = color * 0.5f;
        Gizmos.DrawLine(lastPoint, end);
    }
}