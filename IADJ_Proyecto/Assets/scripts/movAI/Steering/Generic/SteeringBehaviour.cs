using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(AgentNPC))]
public class SteeringBehaviour : MonoBehaviour
{

    protected string nameSteering = "no steering";

    public float weight = 1f;
    public string NameSteering
    {
        set { nameSteering = value; }
        get { return nameSteering; }
    }

    public float Weight
    {
        set { weight = value; }
        get { return weight; }
    }  

    public virtual void NewTarget(Agent target)
    {
        // Este método se puede usar para actualizar el target de un steering
        // por ejemplo, para el Seek, el target es el punto al que se quiere llegar.
        // Para el Flee, el target es el punto del que se quiere huir.
    }

    /// <summary>
    /// Cada SteerinBehaviour retornará un Steering=(vector, escalar)
    /// acorde a su propósito: llegar, huir, pasear, ...
    /// Sobreescribie siempre este método en todas las clases hijas.
    /// </summary>
    /// <param name="agent"></param>
    /// <returns></returns>
    public virtual Steering GetSteering(AgentNPC agent)
    {
        return null;
    }

    public virtual void DestroyVirtual(Agent agent)
    {
        // Este método se puede usar para destruir el steering
        // por ejemplo, para el Seek, el steering se destruye cuando el agente llega al punto.
        // Para el Flee, el steering se destruye cuando el agente se aleja lo suficiente del punto.
    }


    protected virtual void OnGUI()
    {
        // Para la depuración te puede interesar que se muestre el nombre
        // del steeringbehaviour sobre el personaje.
        // Te puede ser util Rect() y GUI.TextField()
        // https://docs.unity3d.com/ScriptReference/GUI.TextField.html
    }
}
