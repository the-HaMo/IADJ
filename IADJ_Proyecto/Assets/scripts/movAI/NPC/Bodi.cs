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

    protected Vector3 _acceleration; // aceleración lineal
    protected float _angularAcc;  // aceleración angular
    protected Vector3 _velocity; // velocidad lineal
    protected float _rotation;  // velocidad angular
    protected float _speed;  // velocidad escalar
    protected float _orientation;  // 'posición' angular
    // Se usará transform.position como 'posición' lineal

    /// Un ejemplo de cómo construir una propiedad en C#
    /// <summary>
    /// Mass for the NPC
    /// </summary>
    public float Mass
    {
        get { return _mass; }
        set { _mass = Mathf.Max(0, value); }
    }

    // CONSTRUYE LAS PROPIEDADES SIGUENTES. PUEDES CAMBIAR LOS NOMBRE A TU GUSTO
    // Lo importante es controlar el set
    // public float MaxForce
    // public float MaxSpeed
    // public Vector3 Velocity

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
        set { Vector3 velocity = Vector3.ClampMagnitude(value, _maxSpeed);
              if (velocity.magnitude < 0.03f) _velocity = Vector3.zero;
                else _velocity = velocity; 
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
        set { float rotation = Mathf.Clamp(value, -_maxRotation, _maxRotation);
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
        get {return new Vector3(_acceleration.x, _acceleration.y, _acceleration.z);}
        set { 
            Vector3 acceleration = Vector3.ClampMagnitude(value, _maxAcceleration);
            if (acceleration.magnitude < 0.03f) _acceleration = Vector3.zero;
            else _acceleration = acceleration;
        }
    } 
    public float AngularAcc
    {
        get { return _angularAcc; }
        set { float angularAcc = Mathf.Clamp(value, -_maxAngularAcc, _maxAngularAcc);
              if (angularAcc < 0.03 && angularAcc > -0.03) _angularAcc = 0f;
                else _angularAcc = angularAcc; 
            }
    }
    // public Vector3 Position. Recuerda. Esta es la única propiedad que trabaja sobre transform.
    public Vector3 Position
    {
        get { return transform.position; }
        set {   value.y = 0; // Para que el movimiento sea en 2.5D
                transform.position = value; 
            }
    }

    public float Orientation
    {
        get { return _orientation; }
        set { _orientation = MapToRange(value, Range.Degrees); 
              transform.rotation = new Quaternion(); 
              transform.Rotate(Vector3.up, _orientation); // transform.Rotate(0, _orientation, 0);               
            }
    }
    public float Speed
    {
        get { return _speed; }
        set { float speed = Mathf.Max(0, _maxSpeed);
              if (speed < 0.03f) _speed = 0f;
                else _speed = speed; 
            }
    }  

    public float MaxAngularAcc
    {
        get { return _maxAngularAcc; }
        set { _maxAngularAcc = Mathf.Max(0, value); }
    }

    // TE PUEDEN INTERESAR LOS SIGUIENTES MÉTODOS.
    // Añade todos los que sean referentes a la parte física.

    // public float Heading()
    //      Retorna el ángulo heading en (-180, 180) en grado o radianes. Lo que consideres
    public static float MapToRange(float rotation, Range r)
{
    rotation %= 360;
    // Si el ángulo es mayor a 180, restamos 360 para obtener su equivalente negativo
    if (rotation > 180) 
        rotation -= 360;
    
    // Si el ángulo es menor o igual a -180, sumamos 360 para obtener el positivo
    else if (rotation <= -180) 
        rotation += 360;

    // 3. Retornamos según la unidad solicitada
    if (r == Range.Degrees) 
    {
        return rotation; 
    }
    else // Radians
    {
        return rotation * Mathf.Deg2Rad;
    }
}
    // public float MapToRange(Range r)
    //      Retorna la orientación de este bodi, un ángulo de (-180, 180), a (0, 360) expresado en grado or radianes
    // public float PositionToAngle()
    //      Retorna el ángulo de una posición usando el eje Z como el primer eje
    public Vector3 OrientationToVector(float angle)
    {
        // Retorna un vector a partir de una orientación usando Z como primer eje
        float rad = angle * Mathf.Deg2Rad;
        float coordX = Mathf.Sin(rad);
        float coordZ = Mathf.Cos(rad);
        return new Vector3(coordX, 0, coordZ);  
    }
    
    // public Vector3 VectorHeading()  // Nombre alternativo
    //      Retorna un vector a partir de una orientación usando Z como primer eje
    // public float GetMiniminAngleTo(Vector3 rotation)
    //      Determina el menor ángulo en 2.5D para que desde la orientación actual mire en la dirección del vector dado como parámetro
    // public void ResetOrientation()
    //      Resetea la orientación del bodi
    // public float PredictNearestApproachTime(Bodi other, float timeInit, float timeEnd)
    //      Predice el tiempo hasta el acercamiento más cercano entre este y otro vehículo entre B y T (p.e. [0, Mathf.Infinity])
    // public float PredictNearestApproachDistance3(Bodi other, float timeInit, float timeEnd)

}
