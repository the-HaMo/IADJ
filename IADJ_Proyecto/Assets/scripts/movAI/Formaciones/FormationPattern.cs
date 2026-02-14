using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FormationPattern
{
    // Número de agentes que acepta la formación
    protected int numAgents;
    
    // Celdas que usa la formación (coordenadas en el grid)
    protected (int, int)[] validSlots;
    
    // Celda del líder
    protected (int, int) leaderSlot;
    
    // Orientaciones que tienen que tener los NPCs de cada celda
    protected float[] relativeAngles;

    public (int, int) GetLeaderSlot()
    {
        return leaderSlot;
    }

    public (int, int) GetSlot(int numSlot)
    {
        return validSlots[numSlot - 1];
    }

    public (int, int)[] GetValidSlots()
    {
        return validSlots;
    }

    public bool SupportAgent(int slotCount)
    {
        return slotCount < numAgents;
    }

    public float GetAngle(int numSlot)
    {
        return relativeAngles[numSlot];
    }

    public int GetNumAgents()
    {
        return numAgents;
    }
}
