using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Steering/InteractiveObject/Agent")]
public class Agent : Bodi // Asegúrate de que la clase Bodi esté definida en tu proyecto
{
    public float lookahead = 5f;
    public float avoidDistance = 3.5f;    
    
    [Tooltip("Radio interior de la IA")]
    [SerializeField] protected float interiorRadius = 1f;

    [Tooltip("Radio de llegada de la IA")]
    [SerializeField] protected float arrivalRadius = 3f;

    [Tooltip("Ángulo interior de la IA")]
    [SerializeField] protected float interiorAngle = 3.0f; // ángulo sexagesimal.

    [Tooltip("Ángulo exterior de la IA")]
    [SerializeField] protected float exteriorAngle = 8.0f; // ángulo sexagesimal.

    // Control de depuración visual
    public bool gizmos = true;

    // Control de terreno
    protected float terrainMultiplier = 1f;

    #region Propiedades
    public float InteriorRadius 
    { 
        get { return interiorRadius; } 
        set { interiorRadius = Mathf.Clamp(value, 0, arrivalRadius); } 
    }    

    public float ArrivalRadius 
    { 
        get { return arrivalRadius; } 
        set { arrivalRadius = Mathf.Max(interiorRadius, value); } 
    }

    public float InteriorAngle 
    { 
        get { return interiorAngle; } 
        set { interiorAngle = Mathf.Clamp(value, 0, exteriorAngle); } 
    }

    public float ExteriorAngle 
    { 
        get { return exteriorAngle; } 
        set { exteriorAngle = Mathf.Clamp(value, interiorAngle, 180.0f); } 
    }

    public new float MaxSpeed
    {
        get { return base.MaxSpeed * terrainMultiplier; }
        set { base.MaxSpeed = value; }
    }

    public new Vector3 Velocity
    {
        get { return base.Velocity; }
        set { 
            if (allowVerticalMovement)
            {
                Vector3 horizontalVel = new Vector3(value.x, 0, value.z);
                horizontalVel = Vector3.ClampMagnitude(horizontalVel, MaxSpeed);
                base.Velocity = new Vector3(horizontalVel.x, value.y, horizontalVel.z);
            }
            else
            {
                // Modo normal: limitamos toda la magnitud
                base.Velocity = Vector3.ClampMagnitude(value, MaxSpeed); 
            }
        }
    }
    #endregion

    #region Métodos Fábrica
    public static Agent CreateStaticVirtual(Vector3 pos, float intRadius = 1f, float arrRadius = 3f, float ori = 0f, bool paint = true) {
        GameObject virt = new GameObject("StaticVirtualAgent");
        virt.AddComponent<BoxCollider>();
        virt.GetComponent<Collider>().isTrigger = true;
        Agent cuerpo = virt.AddComponent<Agent>();
        
        // Usamos las propiedades para asegurar el Clamp
        cuerpo.InteriorRadius = intRadius;
        cuerpo.ArrivalRadius = arrRadius;
        cuerpo.ExteriorAngle = 50f;
        cuerpo.InteriorAngle = 5f;
        
        // Propiedades heredadas de Bodi
        cuerpo.Acceleration = Vector3.zero;
        cuerpo.AngularAcc = 0.0f;
        cuerpo.Velocity = Vector3.zero;
        cuerpo.Rotation = 0.0f;
        cuerpo.Position = pos;
        cuerpo.Orientation = ori;
        cuerpo.gizmos = paint;
        
        return cuerpo;
    }

    public Agent CreateVirtual(Vector3 pos, float intRadius = -1, float arrRadius = -1, float ori = -190, bool paint = true)
    {
        GameObject virt = new GameObject("VirtualAgent");
        virt.AddComponent<BoxCollider>();
        virt.GetComponent<Collider>().isTrigger = true;
        Agent cuerpo = virt.AddComponent<Agent>();
        
        cuerpo.InteriorRadius = (intRadius == -1) ? InteriorRadius : intRadius;
        cuerpo.ArrivalRadius = (arrRadius == -1) ? ArrivalRadius : arrRadius;
        cuerpo.ExteriorAngle = ExteriorAngle;
        cuerpo.InteriorAngle = InteriorAngle;
        
        cuerpo.Acceleration = Vector3.zero;
        cuerpo.AngularAcc = 0.0f;
        cuerpo.Velocity = Vector3.zero;
        cuerpo.Rotation = 0.0f;
        cuerpo.Position = pos;
        
        if (ori != -190) cuerpo.Orientation = ori;
        else cuerpo.Orientation = Orientation;
        
        cuerpo.gizmos = paint;
        return cuerpo;
    }
    #endregion

    #region Actualización
    public void UpdateVirtual(Agent cuerpo, Vector3 pos, float ori = -190f)
    {
        cuerpo.InteriorRadius = InteriorRadius;
        cuerpo.ArrivalRadius = ArrivalRadius;
        cuerpo.ExteriorAngle = ExteriorAngle;
        cuerpo.InteriorAngle = InteriorAngle;
        
        cuerpo.Acceleration = Vector3.zero;
        cuerpo.AngularAcc = 0.0f;
        cuerpo.Velocity = Vector3.zero;
        cuerpo.Rotation = 0.0f;
        cuerpo.Position = pos;
        
        if (ori != -190f) cuerpo.Orientation = ori;
        else cuerpo.Orientation = Orientation;
    }

    public void UpdateVirtual(Vector3 pos, float ori = -190f)
    {
        Position = pos;
        if (ori != -190f) Orientation = ori;
    }

    protected virtual void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 0.5f))
        {
            switch (hit.collider.tag)
            {
                case "RIO":
                    terrainMultiplier = 0.2f; // Velocidad a Reducir
                    break;
                default:
                    terrainMultiplier = 1f;
                    break;
            }
        }
        else
        {
            terrainMultiplier = 1f;
        }
    }
    #endregion

    protected virtual void OnDrawGizmos()
    {
        if (gizmos) 
        {        
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interiorRadius);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, arrivalRadius);
            
            Gizmos.color = Color.red;
            // Vector3.forward es (0,0,1), esto dibuja una línea hacia adelante del mundo
            Gizmos.DrawLine(transform.position, transform.position + 2 * Vector3.forward);
            
            // Dibujado de los conos de visión/ángulos
            Gizmos.color = Color.black;
            Vector3 exteriorPos = OrientationToVector(ExteriorAngle + Orientation);
            Vector3 exteriorNeg = OrientationToVector(-ExteriorAngle + Orientation);
            Gizmos.DrawLine(transform.position, transform.position + 5 * exteriorPos);
            Gizmos.DrawLine(transform.position, transform.position + 5 * exteriorNeg);
            
            Gizmos.color = Color.red;
            Vector3 interiorPos = OrientationToVector(InteriorAngle + Orientation);
            Vector3 interiorNeg = OrientationToVector(-InteriorAngle + Orientation);
            Gizmos.DrawLine(transform.position, transform.position + 5 * interiorPos);
            Gizmos.DrawLine(transform.position, transform.position + 5 * interiorNeg);
            
            Gizmos.color = Color.green;
            Vector3 forward = OrientationToVector(Orientation);
            Gizmos.DrawLine(transform.position, transform.position + 5 * forward);
            
            Gizmos.color = Color.magenta;
            if (Velocity.magnitude > 0.1f)
                Gizmos.DrawLine(Position, Position + Velocity.normalized * lookahead);

            Gizmos.color = terrainMultiplier < 1f ? Color.red : Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * 0.5f);
        }
    }
}