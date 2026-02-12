using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Estructura que representa la asignación de un personaje a una ranura en la formación
/// </summary>
public struct SlotAssignment
{
    public AgentNPC character; // El personaje 
    public int slotNumber;     // El número de ranura asignado 
}

public abstract class FormationPattern : MonoBehaviour
{
    // Define cuántas ranuras puede manejar este patrón
    protected int numberOfSlots;

    /// Retorna la posición y orientación relativa de una ranura específica.
    /// Este método debe ser implementado por cada patrón (V, Círculo, etc).
    public abstract Pose GetSlotLocation(int slotNumber);

    /// Verifica si el patrón soporta añadir un personaje más.
    public abstract bool SupportsSlots(int slotCount);

    /// Calcula el centro de masa de las ranuras ocupadas.
    public virtual Pose GetDriftOffset(List<SlotAssignment> slotAssignments)
    {
        int count = slotAssignments.Count;

        // Si no hay nadie, no hay desviación
        if (count == 0) return new Pose(Vector3.zero, Quaternion.identity);

        Vector3 centerPos = Vector3.zero;
        float centerOrientation = 0f;

        // Sumamos las posiciones de los slots que sí tienen un personaje asignado
        foreach (var assignment in slotAssignments)
        {
            Pose location = GetSlotLocation(assignment.slotNumber);
            centerPos += location.position;
            centerOrientation += location.rotation.eulerAngles.y;
        }

        // Calculamos el promedio (Centro de masas)
        Vector3 averagePos = centerPos / count;
        Quaternion averageRot = Quaternion.Euler(0, centerOrientation / count, 0);

        return new Pose(averagePos, averageRot);
    }
}