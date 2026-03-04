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
    private bool waitingLeaderArrival = false;

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

        CheckLeaderArrival();
        FinishTimer();
    }

    public void Formar()
    {
        if (selectorObjetivos == null)
        {
            Debug.LogError("SeleccionarObjetivos no está asignado!");
            return;
        }

        // Obtener todos los agentes seleccionados
        AgentNPC[] allAgents = ObtenerAgentesSeleccionados();

        Debug.Log($"Intentando formar con {allAgents.Length} agentes seleccionados");

        if (allAgents.Length == 0)
        {
            Debug.LogWarning("No hay agentes seleccionados. Selecciona NPCs primero (clic izquierdo o Shift+clic).");
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
                Debug.Log($"Agente {agent.name} vinculado a celda ({cell.Item1}, {cell.Item2})");
                i++;
            }
        }

        // Verificar componentes necesarios
        VerificarComponentesNPCs(allAgents);

        // Posicionar a todos los agentes usando Leader Following
        grid.LeaderFollowing();
        waitingLeaderArrival = true;
        CheckLeaderArrival();
        
        Debug.Log($"Formación activada con {i} agentes.");
    }

    public void CreatePattern()
    {
        if (tipoFormacion == TipoFormacion.Ataque)
            pattern = new Ataque();
        else
            pattern = new Defensa();
    }

    public void AcabarFormacion()
    {
        if (grid != null)
        {
            NoWait();
            waitingLeaderArrival = false;
            doingWander = false;
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

        if (!Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            return;
        }

        Vector3 point = hit.point;
        AgentNPC[] selectedAgents = ObtenerAgentesSeleccionados();

        if (selectedAgents.Length == 0)
        {
            return;
        }

        if (grid != null && grid.activated)
        {
            bool allSelectedInFormation = true;

            foreach (AgentNPC agent in selectedAgents)
            {
                if (!IsAgentInCurrentFormation(agent))
                {
                    allSelectedInFormation = false;
                    break;
                }
            }

            if (allSelectedInFormation)
            {
                NoWait();
                grid.MoveGrid(point);
                grid.LeaderFollowing();
                waitingLeaderArrival = true;
                Debug.Log($"Formación moviéndose a: {point}");
                return;
            }

            NoWait();
            waitingLeaderArrival = false;
            doingWander = false;
            grid.LiberarAgents();
            Debug.Log("Selección mixta detectada. Se rompe formación y se mueven todos los seleccionados.");
        }

        MoveSelectedAgentsToPoint(selectedAgents, point);
    }

    private bool IsAgentInCurrentFormation(AgentNPC agent)
    {
        if (grid == null || grid.slots == null || agent == null)
        {
            return false;
        }

        for (int i = 0; i < grid.numColumns; i++)
        {
            for (int j = 0; j < grid.numRows; j++)
            {
                if (grid.slots[i, j].npc == agent)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void MoveSelectedAgentsToPoint(AgentNPC[] selectedAgents, Vector3 point)
    {
        int movedCount = 0;
        float spacing = Mathf.Max(1.2f, cellSize * 0.8f);

        for (int index = 0; index < selectedAgents.Length; index++)
        {
            AgentNPC agent = selectedAgents[index];
            Arrive arrive = agent.GetComponent<Arrive>();
            Face face = agent.GetComponent<Face>();
            Align align = agent.GetComponent<Align>();
            Wander wander = agent.GetComponent<Wander>();

            if (arrive == null || face == null)
            {
                Debug.LogWarning($"NPC {agent.name} no tiene Arrive o Face para moverse a punto.");
                continue;
            }

            Vector3 offset = GetOrderedOffset(index, spacing);
            Vector3 targetPoint = point + offset;
            Agent individualTarget = Agent.CreateStaticVirtual(
                targetPoint,
                intRadius: 0.5f,
                arrRadius: 1.5f,
                ori: 0f,
                paint: false
            );

            if (align != null) align.enabled = false;
            if (wander != null) wander.enabled = false;
            arrive.enabled = true;
            face.enabled = true;
            arrive.NewTarget(individualTarget);
            face.NewTarget(individualTarget);
            movedCount++;
        }

        Debug.Log($"{movedCount} NPC(s) distribuidos alrededor de: {point}");
    }

    private Vector3 GetOrderedOffset(int index, float spacing)
    {
        int remaining = index;
        int ring = 1;

        while (true)
        {
            int slotsInRing = ring * 6;
            if (remaining < slotsInRing)
            {
                float angleStep = 360f / slotsInRing;
                float angle = remaining * angleStep;
                float radians = angle * Mathf.Deg2Rad;
                float radius = ring * spacing;
                return new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
            }

            remaining -= slotsInRing;
            ring++;
        }
    }

    private void CheckLeaderArrival()
    {
        if (!waitingLeaderArrival || grid == null || !grid.activated || leader == null)
        {
            return;
        }

        Agent leaderVirtual = grid.GetLeaderSlot().virtualAgent;
        if (leaderVirtual == null)
        {
            return;
        }

        float distance = Vector3.Distance(leader.Position, leaderVirtual.Position);
        if (distance <= leaderVirtual.ArrivalRadius)
        {
            waitingLeaderArrival = false;
            NotifyLeaderArrival();
        }
    }

    private void VerificarComponentesNPCs(AgentNPC[] agentes)
    {
        foreach (AgentNPC agent in agentes)
        {
            Arrive arrive = agent.GetComponent<Arrive>();
            Face face = agent.GetComponent<Face>();
            Align align = agent.GetComponent<Align>();

            string status = $"NPC: {agent.name} - ";
            if (arrive == null) status += "❌ Falta Arrive. ";
            else status += "✓ Arrive. ";
            
            if (face == null) status += "❌ Falta Face. ";
            else status += "✓ Face. ";
            
            if (align == null) status += "❌ Falta Align. ";
            else status += "✓ Align. ";

            if (arrive == null || face == null || align == null)
            {
                Debug.LogWarning(status);
                Debug.LogWarning($"El NPC {agent.name} necesita los componentes Arrive, Face y Align para formar!");
            }
            else
            {
                Debug.Log(status);
            }
        }
    }

    private AgentNPC[] ObtenerAgentesSeleccionados()
    {
        if (selectorObjetivos == null)
        {
            Debug.LogError("selectorObjetivos es null!");
            return new AgentNPC[0];
        }

        List<GameObject> npcsSeleccionados = selectorObjetivos.getListNPCs();
        Debug.Log($"NPCs en lista de selección: {npcsSeleccionados.Count}");
        
        List<AgentNPC> agentes = new List<AgentNPC>();

        foreach (GameObject obj in npcsSeleccionados)
        {
            AgentNPC agent = obj.GetComponent<AgentNPC>();
            if (agent != null)
            {
                agentes.Add(agent);
                Debug.Log($"Agente válido encontrado: {obj.name}");
            }
            else
            {
                Debug.LogWarning($"GameObject {obj.name} no tiene componente AgentNPC!");
            }
        }

        Debug.Log($"Total de agentes válidos: {agentes.Count}");
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
                        grid.AgentsToCell();
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
