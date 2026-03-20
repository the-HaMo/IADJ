using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Range { Degrees, Radians }

public class Bodi : MonoBehaviour
{
    [SerializeField] protected float mass = 1;
    [SerializeField] protected float maxSpeed = 1;
    [SerializeField] protected float maxRotation = 1;
    [SerializeField] protected float maxAcceleration = 1;
    [SerializeField] protected float maxAngularAcc = 1;
    [SerializeField] protected float maxForce = 1;

    protected Vector3 acceleration;
    protected float angularAcc;
    protected Vector3 velocity;
    protected float rotation;
    protected float speed;
    protected float orientation;

    // NUEVO: Flag para permitir movimiento vertical
    [HideInInspector]
    public bool allowVerticalMovement = false;

    public float Mass
    {
        get { return mass; }
        set { mass = Mathf.Max(0, value); }
    }

    public float MaxForce
    {
        get { return maxForce; }
        set { maxForce = Mathf.Max(0, value); }
    }

    public float MaxSpeed
    {
        get { return maxSpeed; }
        set { maxSpeed = Mathf.Max(0, value); }
    }

    public Vector3 Velocity
    {
        get { return new Vector3(velocity.x, velocity.y, velocity.z); } 
        set { 
            // MODIFICADO: Solo limitar la velocidad horizontal si no permitimos movimiento vertical
            if (!allowVerticalMovement)
            {
                Vector3 horizontalVel = new Vector3(value.x, 0, value.z);
                horizontalVel = Vector3.ClampMagnitude(horizontalVel, maxSpeed);
                
                if (horizontalVel.magnitude < 0.03f) 
                    velocity = Vector3.zero;
                else 
                    velocity = horizontalVel;
            }
            else
            {
                // Durante salto: permitir velocidad vertical sin límite
                Vector3 horizontalVel = new Vector3(value.x, 0, value.z);
                horizontalVel = Vector3.ClampMagnitude(horizontalVel, maxSpeed);
                
                velocity = new Vector3(
                    horizontalVel.magnitude < 0.03f ? 0 : horizontalVel.x,
                    value.y, // Mantener Y sin modificar
                    horizontalVel.magnitude < 0.03f ? 0 : horizontalVel.z
                );
            }
        }
    }
    
    public float MaxRotation
    {
        get { return maxRotation; }
        set { maxRotation = Mathf.Max(0, value); }
    }

    public float Rotation
    {
        get { return rotation; }
        set { 
            float r = Mathf.Clamp(value, -maxRotation, maxRotation);
            if (r < 0.03 && r > -0.03) rotation = 0f;
            else rotation = r; 
        }
    }

    public float MaxAcceleration
    {
        get { return maxAcceleration; }
        set { maxAcceleration = Mathf.Max(0, value); }
    }

    public Vector3 Acceleration
    {
        get { return new Vector3(acceleration.x, acceleration.y, acceleration.z); }
        set { 
            Vector3 a = Vector3.ClampMagnitude(value, maxAcceleration);
            if (a.magnitude < 0.03f) acceleration = Vector3.zero;
            else acceleration = a;
        }
    }

    public float AngularAcc
    {
        get { return angularAcc; }
        set { 
            float aA = Mathf.Clamp(value, -maxAngularAcc, maxAngularAcc);
            if (aA < 0.03 && aA > -0.03) angularAcc = 0f;
            else angularAcc = aA; 
        }
    }

    // MODIFICADO: Permitir Y cuando allowVerticalMovement = true
    public Vector3 Position
    {
        get { return transform.position; }
        set { 
            if (!allowVerticalMovement)
            {
                value.y = transform.position.y; 
            }
            transform.position = value; 
        }
    }

    public float Orientation
    {
        get { return orientation; }
        set { 
            orientation = MapToRange(value, Range.Degrees); 
            transform.rotation = new Quaternion(); 
            transform.Rotate(Vector3.up, orientation);               
        }
    }

    public float Speed
    {
        get { return speed; }
        set { 
            float s = Mathf.Max(0, maxSpeed);
            if (s < 0.03f) speed = 0f;
            else speed = s; 
        }
    }

    public float MaxAngularAcc
    {
        get { return maxAngularAcc; }
        set { maxAngularAcc = Mathf.Max(0, value); }
    }

    public static float MapToRange(float rotation, Range r)
    {
        rotation %= 360;
        if (rotation > 180) 
            rotation -= 360;
        else if (rotation <= -180) 
            rotation += 360;

        if (r == Range.Degrees) 
            return rotation; 
        else
            return rotation * Mathf.Deg2Rad;
    }

    public Vector3 OrientationToVector(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float coordX = Mathf.Sin(rad);
        float coordZ = Mathf.Cos(rad);
        return new Vector3(coordX, 0, coordZ);  
    }
}