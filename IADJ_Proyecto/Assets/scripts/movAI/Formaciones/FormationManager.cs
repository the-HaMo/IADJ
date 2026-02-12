using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationManager : MonoBehaviour
{
    public List<SlotAssignment> slotAssignments = new List<SlotAssignment>();
    private Pose driftOffset; // Representa el desplazamiento (posición y orientación)
    public FormationPattern pattern;
    public AgentNPC leader; // El líder de la formación (primer personaje añadido)

    public bool AddCharacter(AgentNPC character)
    {
        int occupiedSlots = slotAssignments.Count;
        if (pattern.SupportsSlots(occupiedSlots + 1))
        {
            // Si es el primer personaje, se convierte en líder
            if (occupiedSlots == 0)
            {
                leader = character;
                Debug.Log($"Líder de formación asignado: {character.name}");
            }

            SlotAssignment newAssignment = new SlotAssignment
            {
                character = character,
                slotNumber = occupiedSlots
            };
            slotAssignments.Add(newAssignment);

            UpdateSlotAssignments();
            return true;
        }

        return false;
    }

    public void RemoveCharacter(AgentNPC character)
    {
        int index = slotAssignments.FindIndex(s => s.character == character); 
        if (index != -1)
        {
            // Si estamos eliminando al líder, disolver la formación
            if (character == leader)
            {
                DisbandFormation();
                return;
            }
            
            // Si no es el líder, solo quitar el personaje
            slotAssignments.RemoveAt(index);
            UpdateSlotAssignments(); 
        }
    }

    public void DisbandFormation()
    {
        slotAssignments.Clear();
        leader = null;
        Debug.Log("Formación disuelta.");
    }

    public void UpdateSlotAssignments()
    {
        for (int i = 0; i < slotAssignments.Count; i++)
        {
            // Asignación secuencial simple 
            var assignment = slotAssignments[i];
            assignment.slotNumber = i;
            slotAssignments[i] = assignment;
        }
        driftOffset = pattern.GetDriftOffset(slotAssignments); 
    }

    private void Update()
    {
        // Actualiza las posiciones de los slots cada frame
        if (pattern != null && leader != null && slotAssignments.Count > 0)
        {
            UpdateSlots();
        }
    }

    public void UpdateSlots()
    {
        // Validaciones
        if (pattern == null)
        {
            Debug.LogWarning("FormationManager: Pattern no asignado");
            return;
        }
        if (leader == null)
        {
            Debug.LogWarning("FormationManager: Líder no asignado");
            return;
        }
        if (slotAssignments.Count == 0)
        {
            return; // No hay nadie en la formación
        }

        // Matriz de rotación 2D
        // Ωl = [cos θ  -sin θ]
        //      [sin θ   cos θ]
        float leaderAngle = leader.Orientation * Mathf.Deg2Rad;
        float leaderOrientation = leader.Orientation; // Guardamos para reutilizar
        float cosAngle = Mathf.Cos(leaderAngle);
        float sinAngle = Mathf.Sin(leaderAngle);

        for (int i = 0; i < slotAssignments.Count; i++)
        {
            // Obtiene la ubicación relativa de la ranura según el patrón 
            Pose relativeLoc = pattern.GetSlotLocation(slotAssignments[i].slotNumber);

            // Aplica matriz de rotación 2D manual (plano XZ)
            // x' = x·cos(θ) - z·sin(θ)
            // z' = x·sin(θ) + z·cos(θ)
            float rotatedX = relativeLoc.position.x * cosAngle - relativeLoc.position.z * sinAngle;
            float rotatedZ = relativeLoc.position.x * sinAngle + relativeLoc.position.z * cosAngle;

            // Transforma la posición relativa a coordenadas globales 
            // Fórmula: ps = pl + Ωl·rs
            Vector3 worldPos = leader.Position + new Vector3(rotatedX, 0, rotatedZ);
            
            // Orientación: ωs = ωl + ωs_relativa
            float relativeOrientation = relativeLoc.rotation.eulerAngles.y;
            float worldOrientation = leaderOrientation + relativeOrientation; // Reutilizamos leaderOrientation
            Quaternion worldRot = Quaternion.Euler(0, worldOrientation, 0);

            // Integración con sistema de Steering usando agente virtual
            AgentNPC character = slotAssignments[i].character;
            if (character != null)
            {
                // Crear un agente virtual en la posición objetivo del slot
                // El behavior de Arrive/Seek del personaje se dirigirá a este agente virtual
                Arrive arriveComponent = character.GetComponent<Arrive>();
                if (arriveComponent != null)
                {
                    // Crear agente virtual: CreateStaticVirtual(posición, radioInterior, radioLlegada, orientación)
                    Agent virtualTarget = Agent.CreateStaticVirtual(worldPos, 1f, 3f, worldOrientation);
                    arriveComponent.NewTarget(virtualTarget);
                }
                else
                {
                    // Si no tiene Arrive, intentar con Seek
                    Seek seekComponent = character.GetComponent<Seek>();
                    if (seekComponent != null)
                    {
                        Agent virtualTarget = Agent.CreateStaticVirtual(worldPos, 1f, 3f, worldOrientation);
                        seekComponent.NewTarget(virtualTarget);
                    }
                }
            }
        }
    }
}