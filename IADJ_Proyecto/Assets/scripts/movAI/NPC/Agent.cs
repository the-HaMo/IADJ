using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Steering/InteractiveObject/Agent")]
public class Agent : Bodi // Asegúrate de que la clase Bodi esté definida en tu proyecto
{
    public float lookahead = 5f;
    public float avoidDistance = 3.5f;    
    
    [Tooltip("Radio interior de la IA")]
    [SerializeField] protected float _interiorRadius = 1f;

    [Tooltip("Radio de llegada de la IA")]
    [SerializeField] protected float _arrivalRadius = 3f;

    [Tooltip("Ángulo interior de la IA")]
    [SerializeField] protected float _interiorAngle = 3.0f; // ángulo sexagesimal.

    [Tooltip("Ángulo exterior de la IA")]
    [SerializeField] protected float _exteriorAngle = 8.0f; // ángulo sexagesimal.

    // Control de depuración visual
    public bool gizmos = true;

    #region Propiedades
    public float InteriorRadius 
    { 
        get { return _interiorRadius; } 
        set { _interiorRadius = Mathf.Clamp(value, 0, _arrivalRadius); } 
    }    

    public float ArrivalRadius 
    { 
        get { return _arrivalRadius; } 
        set { _arrivalRadius = Mathf.Max(_interiorRadius, value); } 
    }

    public float InteriorAngle 
    { 
        get { return _interiorAngle; } 
        set { _interiorAngle = Mathf.Clamp(value, 0, _exteriorAngle); } 
    }

    public float ExteriorAngle 
    { 
        get { return _exteriorAngle; } 
        set { _exteriorAngle = Mathf.Clamp(value, _interiorAngle, 180.0f); } 
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
    #endregion

    protected virtual void OnDrawGizmos()
    {
        if (gizmos) 
        {        
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interiorRadius);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _arrivalRadius);
            
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
        }
    }
}