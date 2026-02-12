using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador para activar/desactivar formaciones con teclas
/// F = Formar en formación
/// ESPACIO = Romper formación y seguir al líder
/// </summary>
public class FormationController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El FormationManager que controla la formación")]
    public FormationManager formationManager;

    [Header("Personajes")]
    [Tooltip("Lista de todos los personajes que pueden participar en la formación")]
    public List<AgentNPC> characters = new List<AgentNPC>();

    [Header("Estado")]
    public bool isInFormation = false;

    private void Update()
    {
        // F = Formar en formación
        if (Input.GetKeyDown(KeyCode.F))
        {
            FormFormation();
        }

        // ESPACIO = Romper formación y seguir al líder
        if (Input.GetKeyDown(KeyCode.Space))
        {
            BreakFormationAndFollowLeader();
        }
    }

    /// <summary>
    /// Forma la formación con todos los personajes
    /// El primer personaje de la lista será el líder
    /// </summary>
    void FormFormation()
    {
        if (formationManager == null)
        {
            Debug.LogError("FormationController: No hay FormationManager asignado");
            return;
        }

        if (characters.Count < 2)
        {
            Debug.LogWarning("FormationController: Se necesitan al menos 2 personajes para formar");
            return;
        }

        Debug.Log("Formando en formación...");

        // Añadir todos los personajes a la formación
        foreach (AgentNPC character in characters)
        {
            if (character != null)
            {
                formationManager.AddCharacter(character);
            }
        }

        isInFormation = true;
        Debug.Log($"Formación activada con {characters.Count} personajes. Líder: {formationManager.leader?.name}");
    }

    /// <summary>
    /// Rompe la formación y hace que todos sigan directamente al líder
    /// </summary>
    void BreakFormationAndFollowLeader()
    {
        if (formationManager == null || formationManager.leader == null)
        {
            Debug.LogWarning("FormationController: No hay formación activa para romper");
            return;
        }

        Debug.Log("Rompiendo formación y siguiendo al líder...");

        AgentNPC leader = formationManager.leader;

        // Crear un agente virtual en la posición del líder para que todos lo sigan
        Agent leaderTarget = Agent.CreateStaticVirtual(leader.Position, 1f, 3f, leader.Orientation);

        // Hacer que cada personaje (excepto el líder) siga al líder directamente
        foreach (AgentNPC character in characters)
        {
            if (character != null && character != leader)
            {
                // Intentar con Arrive primero
                Arrive arriveComponent = character.GetComponent<Arrive>();
                if (arriveComponent != null)
                {
                    arriveComponent.NewTarget(leaderTarget);
                }
                else
                {
                    Seek seekComponent = character.GetComponent<Seek>();
                    if (seekComponent != null)
                    {
                        seekComponent.NewTarget(leaderTarget);
                    }
                }
            }
        }

        // Disolver la formación
        formationManager.DisbandFormation();
        isInFormation = false;

        Debug.Log($"Formación disuelta. Todos siguiendo a: {leader.name}");
    }

    /// <summary>
    /// Método auxiliar para añadir un personaje a la lista
    /// </summary>
    public void AddCharacterToList(AgentNPC character)
    {
        if (!characters.Contains(character))
        {
            characters.Add(character);
        }
    }

    /// <summary>
    /// Método auxiliar para quitar un personaje de la lista
    /// </summary>
    public void RemoveCharacterFromList(AgentNPC character)
    {
        characters.Remove(character);
    }
}
