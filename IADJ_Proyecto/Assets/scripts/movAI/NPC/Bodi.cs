using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Range { Degrees, Radians }

public class Bodi : MonoBehaviour
{
    [SerializeField] protected float _mass = 1;
    [SerializeField] protected float _maxSpeed = 1;
    [SerializeField] protected float _maxRotation = 1;
    [SerializeField] protected float _maxAcceleration = 1;
    [SerializeField] protected float _maxAngularAcc = 1;
    [SerializeField] protected float _maxForce = 1;

    protected Vector3 _acceleration;
    protected float _angularAcc;
    protected Vector3 _velocity;
    protected float _rotation;
    protected float _speed;
    protected float _orientation;

    // NUEVO: Flag para permitir movimiento vertical
    [HideInInspector]
    public bool allowVerticalMovement = false;

    public float Mass
    {
        get { return _mass; }
        set { _mass = Mathf.Max(0, value); }
    }

    public float MaxForce
    {
        get { return _maxForce; }
        set { _maxForce = Mathf.Max(0, value); }
    }

    public float MaxSpeed
    {
        get { return _maxSpeed; }
        set { _maxSpeed = Mathf.Max(0, value); }
    }

    public Vector3 Velocity
    {
        get { return new Vector3(_velocity.x, _velocity.y, _velocity.z); } 
        set { 
            // MODIFICADO: Solo limitar la velocidad horizontal si no permitimos movimiento vertical
            if (!allowVerticalMovement)
            {
                Vector3 horizontalVel = new Vector3(value.x, 0, value.z);
                horizontalVel = Vector3.ClampMagnitude(horizontalVel, _maxSpeed);
                
                if (horizontalVel.magnitude < 0.03f) 
                    _velocity = Vector3.zero;
                else 
                    _velocity = horizontalVel;
            }
            else
            {
                // Durante salto: permitir velocidad vertical sin límite
                Vector3 horizontalVel = new Vector3(value.x, 0, value.z);
                horizontalVel = Vector3.ClampMagnitude(horizontalVel, _maxSpeed);
                
                _velocity = new Vector3(
                    horizontalVel.magnitude < 0.03f ? 0 : horizontalVel.x,
                    value.y, // Mantener Y sin modificar
                    horizontalVel.magnitude < 0.03f ? 0 : horizontalVel.z
                );
            }
        }
    }
    
    public float MaxRotation
    {
        get { return _maxRotation; }
        set { _maxRotation = Mathf.Max(0, value); }
    }

    public float Rotation
    {
        get { return _rotation; }
        set { 
            float rotation = Mathf.Clamp(value, -_maxRotation, _maxRotation);
            if (rotation < 0.03 && rotation > -0.03) _rotation = 0f;
            else _rotation = rotation; 
        }
    }

    public float MaxAcceleration
    {
        get { return _maxAcceleration; }
        set { _maxAcceleration = Mathf.Max(0, value); }
    }

    public Vector3 Acceleration
    {
        get { return new Vector3(_acceleration.x, _acceleration.y, _acceleration.z); }
        set { 
            Vector3 acceleration = Vector3.ClampMagnitude(value, _maxAcceleration);
            if (acceleration.magnitude < 0.03f) _acceleration = Vector3.zero;
            else _acceleration = acceleration;
        }
    }

    public float AngularAcc
    {
        get { return _angularAcc; }
        set { 
            float angularAcc = Mathf.Clamp(value, -_maxAngularAcc, _maxAngularAcc);
            if (angularAcc < 0.03 && angularAcc > -0.03) _angularAcc = 0f;
            else _angularAcc = angularAcc; 
        }
    }

    // MODIFICADO: Permitir Y cuando allowVerticalMovement = true
    public Vector3 Position
    {
        get { return transform.position; }
        set { 
            if (!allowVerticalMovement)
            {
                value.y = 0; // Mantener en el suelo en modo normal
            }
            // Si allowVerticalMovement = true, mantener Y del value
            transform.position = value; 
        }
    }

    public float Orientation
    {
        get { return _orientation; }
        set { 
            _orientation = MapToRange(value, Range.Degrees); 
            transform.rotation = new Quaternion(); 
            transform.Rotate(Vector3.up, _orientation);               
        }
    }

    public float Speed
    {
        get { return _speed; }
        set { 
            float speed = Mathf.Max(0, _maxSpeed);
            if (speed < 0.03f) _speed = 0f;
            else _speed = speed; 
        }
    }

    public float MaxAngularAcc
    {
        get { return _maxAngularAcc; }
        set { _maxAngularAcc = Mathf.Max(0, value); }
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