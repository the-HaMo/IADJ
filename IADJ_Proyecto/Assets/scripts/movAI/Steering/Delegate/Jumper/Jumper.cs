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
    [HideInInspector]
    public float landingTolerance = 1f;
    
    [Tooltip("Altura mínima sobre el suelo para considerar que estamos en el aire")]
    public float airborneHeight = 0.5f;

    [HideInInspector]
    public bool autoResetOnComplete = true;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    [HideInInspector] public bool showDebugLogs = true;
    [HideInInspector] public Color jumpPointColor = Color.yellow;
    [HideInInspector] public Color landingPointColor = Color.green;
    [HideInInspector] public Color activeJumpColor = Color.red;

    // Estado interno
    private JumpTrigger activeJump = null;
    private Vector3 calculatedVelocity;
    private Vector3 currentVelocity;
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
        // El modo de salto ahora es DirectVelocity por defecto siempre
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        if (jumpTriggers == null || jumpTriggers.Count == 0)
        {
            return steer;
        }

        if (!isExecutingJump)
        {
            activeJump = DetectNearestJumpTrigger(agent);
            
            if (activeJump != null)
            {
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
                
                activeJump.hasBeenUsed = true;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[Jump] ========== INICIO DE SALTO ==========");
                    Debug.Log($"[Jump] Desde: {activeJump.jumpPoint.name} → {activeJump.landingPoint.name}");
                    Debug.Log($"[Jump] Posición inicial: {agent.Position}");
                    Debug.Log($"[Jump] Velocidad calculada: {calculatedVelocity}");
                }
            }
        }

        if (isExecutingJump && activeJump != null)
        {
            timeInAir += Time.deltaTime;

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
                }
                
                if (autoResetOnComplete && AllJumpsCompleted())
                {
                    ResetAllJumps();
                }
                
                activeJump = null;
                return steer;
            }

            // Aplicar siempre DirectVelocity
            return ApplyDirectVelocityJump(agent);
        }

        return steer;
    }

    private Steering ApplyDirectVelocityJump(AgentNPC agent)
    {
        Steering steer = new Steering();

        if (!impulseApplied)
        {
            agent.allowVerticalMovement = true;
            agent.Velocity = new Vector3(calculatedVelocity.x, calculatedVelocity.y, calculatedVelocity.z);
            impulseApplied = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[Jump] Impulso aplicado - Velocidad: {agent.Velocity}");
            }
        }
        else
        {
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
        }
        
        float sqrtDisc = Mathf.Sqrt(Mathf.Abs(discriminant));
        float time1 = (-b + sqrtDisc) / (2 * a);
        float time2 = (-b - sqrtDisc) / (2 * a);
        
        float time = 0;
        if (time1 > 0 && time2 > 0) time = Mathf.Min(time1, time2);
        else if (time1 > 0) time = time1;
        else if (time2 > 0) time = time2;
        else time = Mathf.Sqrt(2 * Mathf.Abs(deltaY) / gravity);
        
        time = Mathf.Max(time, 0.1f);
        
        float vx = deltaX / time;
        float vz = deltaZ / time;
        
        float horizontalSpeed = Mathf.Sqrt(vx * vx + vz * vz);
        
        if (horizontalSpeed > maxSpeed)
        {
            float scale = maxSpeed / horizontalSpeed;
            vx *= scale;
            vz *= scale;
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
            agent.allowVerticalMovement = false;
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

    public bool IsJumping() { return isExecutingJump; }
    public bool HasLanded() { return hasLanded; }
    public JumpTrigger GetActiveJump() { return activeJump; }

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
            UnityEditor.Handles.Label(trigger.jumpPoint.position + Vector3.up * 1.5f, $"JUMP {jumpTriggers.IndexOf(trigger)}", style);
            #endif
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
            if (point.y < end.y - 1f) break;
            lastPoint = point;
        }
        Gizmos.color = color * 0.5f;
        Gizmos.DrawLine(lastPoint, end);
    }
}