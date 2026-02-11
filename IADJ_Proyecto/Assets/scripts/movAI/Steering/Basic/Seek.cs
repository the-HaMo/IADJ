using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seek : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour
    [SerializeField] protected Agent target;

    
    void Start()
    {
        this.nameSteering = "Seek";
    }

    public override void NewTarget(Agent t)
    {
       target = t;
    }

    public Agent Target
    {
        get { return target; }
        set { target = value; }
    }

    public override Steering GetSteering(AgentNPC agent)
    {
        Steering steer = new Steering();

        if (target == null)
        {
            steer.linear = Vector3.zero;
            steer.angular = 0f;
            return steer;
        }

        // Calcula el steering.
        Vector3 origen = agent.Position;
        Vector3 destino = target.Position;
        Vector3 direccion = destino - origen;
        steer.linear = direccion.normalized * agent.MaxSpeed;
        steer.angular = 0f;
        // Retornamos el resultado final.
        return steer;
    }
}