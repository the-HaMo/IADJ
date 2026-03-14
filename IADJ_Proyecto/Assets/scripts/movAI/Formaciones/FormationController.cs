using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum TipoFormacion { Ataque, Defensa }

public enum WanderLoopState
{
    Inactive, WaitingToWander, Wandering, Paused
}

public class FormationController : MonoBehaviour
{
    [Header("Configuración General")]
    public float cellSize = 2.0f;
    public TipoFormacion tipoFormacion = TipoFormacion.Ataque;

    [Header("Bucle Wander del líder")]
    [SerializeField] private bool autoWanderLoop = true;
    [SerializeField] private float waitBeforeWander = 10f;
    [SerializeField] private float wanderDuration = 6f;
    [SerializeField] private float pauseBetweenWanders = 2f;

    // Referencias
    private GridFormation grid;
    private FormationPattern pattern;
    private AgentNPC leader;
    private SeleccionarObjetivos selectorObjetivos;

    // Control de ciclo Wander
    private WanderLoopState wanderLoopState = WanderLoopState.Inactive;
    private float wanderLoopTimer = 0f;
    private bool wanderLoopStoppedByUser = false;
    private bool waitingLeaderArrival = false;

    private const int REQUIRED_AGENTS_COUNT = 6;

    private void Start()
    {
        selectorObjetivos = FindFirstObjectByType<SeleccionarObjetivos>();
        if (selectorObjetivos == null) Debug.LogError("No se encontró SeleccionarObjetivos en la escena.");
    }

    private void Update()
    {
        HandleInputs();
        CheckLeaderArrival();
        UpdateWanderLoop();
    }

    private void HandleInputs()
    {
        if (Input.GetKeyDown(KeyCode.F)) Formar();
        if (Input.GetKeyDown(KeyCode.G)) AcabarFormacion();
        if (Input.GetMouseButtonDown(1)) MoverAPunto();
        if (Input.GetKeyDown(KeyCode.W)) StopWanderLoop();
    }

    // --- LÓGICA CORE DE FORMACIÓN ---

    public void Formar()
    {
        if (selectorObjetivos == null) return;

        AgentNPC[] allAgents = ObtenerAgentesSeleccionados();
        
        if (allAgents.Length < REQUIRED_AGENTS_COUNT)
        {
            Debug.LogWarning($"Se necesitan al menos {REQUIRED_AGENTS_COUNT} NPCs. Hay {allAgents.Length}.");
            return;
        }

        // Si hay más de los necesarios, deseleccionamos el exceso
        if (allAgents.Length > REQUIRED_AGENTS_COUNT)
        {
            for (int idx = REQUIRED_AGENTS_COUNT; idx < allAgents.Length; idx++)
            {
                selectorObjetivos.DeseleccionarNPC(allAgents[idx].gameObject);
            }
            allAgents = ObtenerAgentesSeleccionados();
        }

        wanderLoopStoppedByUser = false;
        NoWait();

        // Limpiar grid anterior si existe
        if (grid != null)
        {
            grid.LiberarAgents();
            Destroy(grid);
        }

        leader = allAgents[0];
        pattern = tipoFormacion == TipoFormacion.Ataque ? new Ataque() : new Defensa();

        var (leaderI, leaderJ) = pattern.GetLeaderSlot();
        float leaderRelativeAngle = pattern.GetAngle(0);

        grid = gameObject.AddComponent<GridFormation>();
        grid.CreateGridManager(cellSize, leader, leaderI, leaderJ, 0f, 4, 4);

        // Configurar celda del líder
        grid.slots[leaderI, leaderJ].relativeOrientation = leaderRelativeAngle;
        grid.slots[leaderI, leaderJ].virtualAgent.UpdateVirtual(
            grid.slots[leaderI, leaderJ].virtualAgent.Position,
            ori: leaderRelativeAngle
        );

        // Configurar posiciones iniciales de los virtuales
        for (int c = 0; c < grid.numColumns; c++)
            for (int r = 0; r < grid.numRows; r++)
                grid.slots[c, r].virtualAgent.Position = grid.GridToPlane(c, r);

        // Vincular seguidores
        int slotIndex = 1;
        foreach (var agent in allAgents)
        {
            if (agent != leader && pattern.SupportAgent(slotIndex))
            {
                var (col, row) = pattern.GetSlot(slotIndex);
                grid.LinkToSlot(col, row, pattern.GetAngle(slotIndex), agent);
                slotIndex++;
            }
        }

        grid.ReasignarCeldasOcupadas();
        VerificarComponentesNPCs(allAgents);
        
        grid.LeaderFollowing();
        waitingLeaderArrival = true;
    }

    public void AcabarFormacion()
    {
        if (grid == null) return;
        
        NoWait();
        waitingLeaderArrival = false;
        grid.LiberarAgents();
        Debug.Log("Formación disuelta.");
    }

    public void MoverAPunto()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity)) return;

        AgentNPC[] selectedAgents = ObtenerAgentesSeleccionados();
        if (selectedAgents.Length == 0) return;

        // Si todos los seleccionados pertenecen a la formación actual, movemos la formación
        if (grid != null && grid.activated && selectedAgents.All(IsAgentInCurrentFormation))
        {
            NoWait();
            grid.MoveGrid(hit.point);
            grid.LeaderFollowing();
            waitingLeaderArrival = true;
            return;
        }

        // Si es un mix, o no hay formación activa, los movemos individualmente
        if (grid != null && grid.activated)
        {
            AcabarFormacion();
            Debug.Log("Selección mixta. Se rompe formación y se mueven individualmente.");
        }

        MoveSelectedAgentsToPoint(selectedAgents, hit.point);
    }

    // --- LÓGICA INDIVIDUAL ---

    private void MoveSelectedAgentsToPoint(AgentNPC[] selectedAgents, Vector3 point)
    {
        float spacing = Mathf.Max(1.2f, cellSize);

        for (int i = 0; i < selectedAgents.Length; i++)
        {
            AgentNPC agent = selectedAgents[i];
            
            if (!agent.TryGetComponent(out Arrive arrive) || !agent.TryGetComponent(out Face face)) continue;

            // Desactivar otros Steerings conflictivos
            if (agent.TryGetComponent(out Align align)) align.enabled = false;
            if (agent.TryGetComponent(out Wander wander)) wander.enabled = false;

            Agent target = Agent.CreateStaticVirtual(
                point + GetOrderedOffset(i, spacing),
                intRadius: 0.5f, arrRadius: 1.5f, ori: 0f, paint: false
            );

            arrive.enabled = true;
            face.enabled = true;
            arrive.NewTarget(target);
            face.NewTarget(target);
        }
    }

    // Distribuye los objetivos seleccionados en círculos concéntricos ordenados alrededor del punto destino, para evitar que se amontonen
    private Vector3 GetOrderedOffset(int index, float spacing)
    {
        int remaining = index;
        int ring = 1;

        while (true)
        {
            int slotsInRing = ring * 6;
            if (remaining < slotsInRing)
            {
                float radians = (remaining * (360f / slotsInRing)) * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(radians) * (ring * spacing), 0f, Mathf.Sin(radians) * (ring * spacing));
            }
            remaining -= slotsInRing;
            ring++;
        }
    }

    // --- MÁQUINA DE ESTADOS WANDER ---

    public void StartTimer()
    {
        if (!autoWanderLoop || grid == null || !grid.activated)
        {
            NoWait();
            return;
        }

        wanderLoopState = WanderLoopState.WaitingToWander;
        wanderLoopTimer = -Mathf.Max(0f, pauseBetweenWanders);
    }

    public void NoWait()
    {
        wanderLoopState = WanderLoopState.Inactive;
        wanderLoopTimer = 0f;
    }

    private void UpdateWanderLoop()
    {
        if (grid == null || !grid.activated || leader == null || waitingLeaderArrival || !autoWanderLoop || wanderLoopStoppedByUser || wanderLoopState == WanderLoopState.Inactive)
            return;

        wanderLoopTimer += Time.deltaTime;

        switch (wanderLoopState)
        {
            case WanderLoopState.WaitingToWander:
                if (wanderLoopTimer >= waitBeforeWander)
                {
                    grid.LeaderWander();
                    wanderLoopState = WanderLoopState.Wandering;
                    wanderLoopTimer = 0f;
                }
                break;

            case WanderLoopState.Wandering:
                if (wanderLoopTimer >= wanderDuration)
                {
                    grid.StopLeaderWander();
                    grid.MoveGrid(leader.Position);
                    grid.LeaderFollowing();
                    waitingLeaderArrival = true;
                    
                    wanderLoopState = WanderLoopState.Paused;
                    wanderLoopTimer = 0f;
                }
                break;

            case WanderLoopState.Paused:
                if (wanderLoopTimer >= pauseBetweenWanders)
                {
                    wanderLoopState = WanderLoopState.WaitingToWander;
                    wanderLoopTimer = 0f;
                }
                break;
        }
    }

    private void StopWanderLoop()
    {
        if (grid == null || !grid.activated) return;

        wanderLoopStoppedByUser = true;
        NoWait();
        grid.StopLeaderWander();

        if (leader != null)
        {
            grid.MoveGrid(leader.Position);
            grid.LeaderFollowing();
            waitingLeaderArrival = true;
        }
    }

    private void CheckLeaderArrival()
    {
        if (!waitingLeaderArrival || grid == null || !grid.activated || leader == null) return;

        Agent leaderVirtual = grid.GetLeaderSlot().virtualAgent;
        if (leaderVirtual != null && Vector3.Distance(leader.Position, leaderVirtual.Position) <= leaderVirtual.ArrivalRadius)
        {
            waitingLeaderArrival = false;
            grid.AgentsToCell();
            StartTimer();
        }
    }

    // --- UTILS ---

    public FormationPattern GetPattern() => pattern;

    private bool IsAgentInCurrentFormation(AgentNPC agent)
    {
        if (grid == null || grid.slots == null || agent == null) return false;

        foreach (var slot in grid.slots)
            if (slot.npc == agent) return true;

        return false;
    }

    private AgentNPC[] ObtenerAgentesSeleccionados()
    {
        if (selectorObjetivos == null) return new AgentNPC[0];

        return selectorObjetivos.getListNPCs()
            .Select(obj => obj.GetComponent<AgentNPC>())
            .Where(agent => agent != null)
            .ToArray();
    }

    private void VerificarComponentesNPCs(AgentNPC[] agentes)
    {
        foreach (AgentNPC agent in agentes)
        {
            bool hasArrive = agent.GetComponent<Arrive>() != null;
            bool hasFace = agent.GetComponent<Face>() != null;
            bool hasAlign = agent.GetComponent<Align>() != null;

            if (!hasArrive || !hasFace || !hasAlign)
            {
                Debug.LogWarning($"El NPC {agent.name} necesita Arrive, Face y Align. (Arrive:{hasArrive}, Face:{hasFace}, Align:{hasAlign})");
            }
        }
    }
}