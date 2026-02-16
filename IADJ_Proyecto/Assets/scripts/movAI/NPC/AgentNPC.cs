using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AgentNPC : Agent
{ 
    [SerializeField] protected Steering steer;
    private List<SteeringBehaviour> listSteerings;

    // NUEVO: Sistema de gravedad
    [Header("Physics")]
    [Tooltip("Aplicar gravedad cuando está en el aire")]
    public bool useGravity = true;
    
    [Tooltip("Gravedad personalizada (0 = usar Physics.gravity)")]
    public float customGravity = 0f;
    
    [Tooltip("Altura del suelo")]
    public float groundLevel = 0f;

    protected void Awake()
    {
        this.steer = new Steering();
        this.listSteerings = GetComponents<SteeringBehaviour>().ToList();
    }

    void Start()
    {
        this.Velocity = Vector3.zero;
    }

    public virtual void Update()
    {
        ApplySteering(Time.deltaTime);
    }

    private void ApplySteering(float deltaTime)
    {
        // Actualizar aceleraciones
        Acceleration = this.steer.linear;
        AngularAcc = this.steer.angular;
        
        // Actualizar velocidades
        Velocity += Acceleration * deltaTime;
        Rotation += AngularAcc * deltaTime;
        
        // NUEVO: Aplicar gravedad si está en el aire
        if (useGravity && allowVerticalMovement)
        {
            float gravity = customGravity > 0 ? customGravity : Mathf.Abs(Physics.gravity.y);
            Vector3 currentVel = Velocity;
            currentVel.y -= gravity * deltaTime;
            Velocity = currentVel;
        }
        
        // Actualizar posición
        Position += Velocity * deltaTime;
        
        // NUEVO: Verificar colisión con el suelo
        if (Position.y <= groundLevel && allowVerticalMovement)
        {
            // Aterrizar
            Vector3 pos = Position;
            pos.y = groundLevel;
            Position = pos;
            
            // Detener velocidad vertical
            Vector3 vel = Velocity;
            vel.y = 0;
            Velocity = vel;
            
            // Desactivar movimiento vertical
            allowVerticalMovement = false;
        }
        
        // Actualizar orientación
        Orientation += Rotation * deltaTime;
        transform.rotation = new Quaternion();
        transform.Rotate(Vector3.up, Orientation);
    }

    public virtual void LateUpdate()
    {
        Steering kinematicFinal = new Steering();
        this.steer = new Steering();

        foreach (SteeringBehaviour behavior in listSteerings)
        {
            if (behavior != null)
            {
                Steering kinematic = behavior.GetSteering(this);
                kinematicFinal.linear += kinematic.linear * behavior.weight;
                kinematicFinal.angular += kinematic.angular * behavior.weight;
            }
        }

        this.steer = kinematicFinal;
    }
}