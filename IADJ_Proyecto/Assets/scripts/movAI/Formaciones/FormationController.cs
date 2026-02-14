using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum TipoFormacion
{
    Ataque,
    Defensa
}

public enum Criterio
{
    LeaderFollowing
    // Pathfinding - Comentado por ahora
}

public class FormationController : MonoBehaviour
{
    // Parámetros de configuración
    public float cellSize = 2.0f;
    public TipoFormacion tipoFormacion = TipoFormacion.Ataque;
    public Criterio criterio = Criterio.LeaderFollowing;

    // Referencias
    private GridFormation grid;
    private FormationPattern pattern;
    private AgentNPC leader;
    private SeleccionarObjetivos selectorObjetivos;

    // Control de tiempo para Wander
    private int inicio;
    private bool waiting = false;
    private bool doingWander = false;

    void Start()
    {
        // Buscar el selector de objetivos en la escena
        selectorObjetivos = FindFirstObjectByType<SeleccionarObjetivos>();
        if (selectorObjetivos == null)
        {
            Debug.LogError("No se encontró SeleccionarObjetivos en la escena. Añade el componente a un GameObject.");
        }
    }

    void Update()
    {
        // Tecla F para formar
        if (Input.GetKeyDown(KeyCode.F))
        {
            Formar();
        }

        // Tecla G para deshacer formación
        if (Input.GetKeyDown(KeyCode.G))
        {
            AcabarFormacion();
        }

        // Clic derecho para mover formación
        if (Input.GetMouseButtonDown(1))
        {
            MoverAPunto();
        }

        FinishTimer();
    }

    public void Formar()
    {
        if (selectorObjetivos == null) return;

        // Obtener todos los agentes seleccionados
        AgentNPC[] allAgents = ObtenerAgentesSeleccionados();

        if (allAgents.Length == 0)
        {
            Debug.LogWarning("No hay agentes seleccionados. Selecciona NPCs primero (clic izquierdo).");
            return;
        }

        // Si todavía no se ha creado el grid, crearlo y prepararlo
        if (grid == null)
        {
            leader = allAgents[0];
            
            // Crear el patrón de formación según el tipo seleccionado
            CreatePattern();

            // Celda del líder en la formación específica
            (int, int) leaderSlot = pattern.GetLeaderSlot();
            float leaderAngle = pattern.GetAngle(0);

            // Crear y preparar el grid
            grid = gameObject.AddComponent<GridFormation>();
            grid.CreateGridManager(cellSize, leader, leaderSlot.Item1, leaderSlot.Item2, leaderAngle, 4, 4);
            
            Debug.Log($"Formación creada con líder: {leader.name}");
        }

        // Mover el grid a la posición del líder si no está ahí
        if (!grid.activated)
        {
            grid.MoveGrid(leader.Position);
            grid.activated = true;
        }

        int i = 1;
        // Añadir cada agente al grid (menos el líder que ya fue añadido)
        foreach (var agent in allAgents)
        {
            if (agent != leader && pattern.SupportAgent(i))
            {
                // Celda que le corresponde en la formación específica
                (int, int) cell = pattern.GetSlot(i);
                float angle = pattern.GetAngle(i);
                
                // Conectar el NPC a la celda correspondiente
                grid.LinkToSlot(cell.Item1, cell.Item2, angle, agent);
                i++;
            }
        }

        // Posicionar a todos los agentes usando Leader Following
        grid.LeaderFollowing();
        
        Debug.Log($"Formación activada con {i} agentes.");
    }

    public void CreatePattern()
    {
        if (tipoFormacion == TipoFormacion.Ataque)
            pattern = new AttackPattern();
        else
            pattern = new DefensivePattern();
    }

    public void AcabarFormacion()
    {
        if (grid != null)
        {
            NoWait();
            grid.LiberarAgents();
            Debug.Log("Formación disuelta.");
        }
    }

    public void NotifyLeaderArrival()
    {
        if (grid != null)
        {
            grid.AgentsToCell();
        }
    }

    public void MoverAPunto()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 point = hit.point;
            
            if (grid != null && grid.activated)
            {
                // Mover la formación
                grid.MoveGrid(point);
                grid.LeaderFollowing();
                Debug.Log($"Formación moviéndose a: {point}");
            }
        }
    }

    private AgentNPC[] ObtenerAgentesSeleccionados()
    {
        List<GameObject> npcsSeleccionados = selectorObjetivos.getListNPCs();
        List<AgentNPC> agentes = new List<AgentNPC>();

        foreach (GameObject obj in npcsSeleccionados)
        {
            AgentNPC agent = obj.GetComponent<AgentNPC>();
            if (agent != null)
            {
                agentes.Add(agent);
            }
        }

        return agentes.ToArray();
    }

    public void StartTimer()
    {
        inicio = Environment.TickCount;
        waiting = true;
    }

    public void NoWait()
    {
        waiting = false;
    }

    public void Wait()
    {
        waiting = true;
    }

    public void FinishTimer()
    {
        if (waiting)
        {
            if ((Environment.TickCount - inicio) > 10000)
            {
                if (doingWander)
                {
                    if (grid != null)
                    {
                        grid.LeaderFollowing();
                        doingWander = false;
                    }
                }
                else
                {
                    if (grid != null)
                    {
                        grid.LeaderWander();
                        doingWander = true;
                    }
                }
                
                StartTimer();
            }
        }
    }

    public void DisactivateGrid()
    {
        if (grid != null)
        {
            grid.activated = false;
        }
    }

    public void BreakFormationFollowLeader()
    {
        if (grid != null)
        {
            NoWait();
            grid.LeaderWander();
            Debug.Log("Formación rota. Los agentes siguen al líder.");
        }
    }

    public void ReformFormation()
    {
        if (grid != null)
        {
            doingWander = false;
            grid.LeaderFollowing();
            Debug.Log("Reformando la formación.");
        }
    }
}
