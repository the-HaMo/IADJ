using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estructura que representa un slot (ranura) en la formación
/// </summary>
public struct Slot
{
    public AgentNPC npc;
    public Vector3 relativePosition;
    public float relativeOrientation;
    public bool leaderCell;
    public Agent virtualAgent;
}

/// <summary>
/// Grid de formación que administra los slots y posiciones de los agentes.
/// Implementa el sistema de Leader Following.
/// </summary>
public class GridFormation : MonoBehaviour
{
    private const float FOLLOWER_FLEE_DISTANCE = 1.2f;
    [SerializeField] private bool debugReubicaciones = true;
    private bool leaderWallAvoidance = false;
    private bool virtualAgentsCleaned = false;

    public int numColumns;
    public int numRows;
    public float cellSize;
    
    public AgentNPC leader;
    public float leaderAngle;
    public Slot[,] slots;
    public Vector3 gridPosition;
    public FormationController formationController;
    public bool activated = false;

    // Referencia directa al slot del líder para evitar bucles
    private Slot leaderSlotRef;

    public void SetLeaderWallAvoidance(bool enabled)
    {
        leaderWallAvoidance = enabled;
    }

    public void CreateGridManager(float cellSize, AgentNPC leader, int leaderI, int leaderJ, float angle, int numColumns, int numRows)
    {
        virtualAgentsCleaned = false;
        this.activated = true;
        this.numColumns = numColumns;
        this.numRows = numRows;
        this.slots = new Slot[numColumns, numRows];
        this.cellSize = cellSize;
        this.leader = leader;
        this.leaderAngle = angle;
        this.gridPosition = leader.Position;
        this.formationController = FindFirstObjectByType<FormationController>();

        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                slots[i, j] = new Slot
                {
                    relativePosition = new Vector3((i - leaderI) * cellSize, 0f, (j - leaderJ) * cellSize),
                    relativeOrientation = 0f,
                    leaderCell = (leaderI == i && leaderJ == j),
                    npc = (leaderI == i && leaderJ == j) ? this.leader : null
                };

                slots[i, j].virtualAgent = Agent.CreateStaticVirtual(
                    GridToPlane(i, j), intRadius: 0.5f, arrRadius: 2f, ori: angle, paint: false
                );

                if (slots[i, j].leaderCell) leaderSlotRef = slots[i, j];
            }
        }
    }

    public void LinkToSlot(int i, int j, float angle, AgentNPC npc)
    {
        slots[i, j].npc = npc;
        slots[i, j].relativeOrientation = angle;
        slots[i, j].virtualAgent.UpdateVirtual(slots[i, j].virtualAgent.Position, ori: leaderAngle + angle);
    }

    /// <summary>
    /// Dada una posición relativa, devuelve la posición de la ranura en el mundo real.
    /// pf = pl + matriz de cambio * pr
    /// </summary>
    public Vector3 GridToPlane(int i, int j)
    {
        // Matriz de cambio de base (Rotación 2D en el plano XZ)
        float radians = Bodi.MapToRange(leaderAngle, Range.Radians);

        float cosAngle = Mathf.Cos(radians);
        float sinAngle = Mathf.Sin(radians);

        Vector3 relativeLoc = slots[i, j].relativePosition; // pr
        
        // matriz de cambio * pr
        float rotatedX = relativeLoc.x * cosAngle - relativeLoc.z * sinAngle;
        float rotatedZ = relativeLoc.x * sinAngle + relativeLoc.z * cosAngle;

        // pf = pl + (matriz * pr)
        return gridPosition + new Vector3(rotatedX, 0, rotatedZ); 
    }

    public void MoveGrid(Vector3 newPosition)
    {
        this.gridPosition = newPosition;
        List<AgentNPC> listaTemporalNPCs = new List<AgentNPC>();

        // 1. Guardar y limpiar
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc != null && slots[i, j].npc != leader)
                {
                    listaTemporalNPCs.Add(slots[i, j].npc);
                }
                slots[i, j].npc = null;
                slots[i, j].leaderCell = false;
            }
        }

        // 2. Restaurar patrón base
        if (formationController != null)
        {
            FormationPattern activePattern = formationController.GetPattern();
            var (leaderI, leaderJ) = activePattern.GetLeaderSlot();

            slots[leaderI, leaderJ].npc = leader;
            slots[leaderI, leaderJ].leaderCell = true;
            slots[leaderI, leaderJ].relativeOrientation = activePattern.GetAngle(0);
            leaderSlotRef = slots[leaderI, leaderJ];

            var validSlots = activePattern.GetValidSlots();
            for (int k = 0; k < listaTemporalNPCs.Count && k < validSlots.Length; k++)
            {
                int f = validSlots[k].Item1;
                int c = validSlots[k].Item2;
                slots[f, c].npc = listaTemporalNPCs[k];
                slots[f, c].relativeOrientation = activePattern.GetAngle(k + 1);
            }
        }

        // 3. Actualizar posiciones virtuales
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                slots[i, j].virtualAgent.Position = GridToPlane(i, j);
                slots[i, j].virtualAgent.Orientation = leaderAngle + slots[i, j].relativeOrientation;
            }
        }

        ReasignarCeldasOcupadas();
    }

    public Slot GetLeaderSlot() => leaderSlotRef;

    // --- MANEJO CENTRALIZADO DE STEERINGS ---
    
    /// <summary>
    /// Apaga todos los comportamientos de steering de un NPC para dejarlo "limpio".
    /// </summary>
    private void DisableAllSteerings(AgentNPC npc)
    {
        SteeringBehaviour[] allSteerings = npc.GetComponents<SteeringBehaviour>();
        foreach (SteeringBehaviour sb in allSteerings)
        {
            sb.enabled = false;
        }
    }

    public void LeaderFollowing()
    {
        Agent leaderVirtual = GetLeaderSlot().virtualAgent;

        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc == null) continue;

                AgentNPC currentNPC = slots[i, j].npc;
                
                // Apagamos TODO primero
                DisableAllSteerings(currentNPC);

                // Si es el líder, solo Arrive y Face hacia el grid
                if (currentNPC == leader)
                {
                    if (currentNPC.TryGetComponent(out Arrive arrive) && currentNPC.TryGetComponent(out Face face))
                    {
                        arrive.enabled = true;
                        face.enabled = true;
                        arrive.NewTarget(leaderVirtual);
                        face.NewTarget(leaderVirtual);
                    }
                }
                // Si es seguidor, Leader Following
                else
                {
                    ConfigureFollowerLeaderFollowing(currentNPC);
                }
            }
        }
    }

    private void ConfigureFollowerLeaderFollowing(AgentNPC follower)
    {
        if (follower.TryGetComponent(out Arrive arrive) && follower.TryGetComponent(out Face face))
        {
            arrive.enabled = true;
            face.enabled = true;
            arrive.NewTarget(leader);
            face.NewTarget(leader);
        }

        if (follower.TryGetComponent(out Separation separation))
        {
            separation.enabled = true;
        }
        
        if (follower.TryGetComponent(out Flee flee))
        {
            bool tooClose = Vector3.Distance(follower.Position, leader.Position) < FOLLOWER_FLEE_DISTANCE;
            flee.enabled = tooClose;
            if (tooClose)
            {
                flee.NewTarget(leader);
            }
        }
    }

    public void AgentsToCell()
    {
        ReasignarCeldasOcupadas();

        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc == null) continue;

                AgentNPC currentNPC = slots[i, j].npc;
                DisableAllSteerings(currentNPC);
                
                if (currentNPC.TryGetComponent(out Arrive arrive) && currentNPC.TryGetComponent(out Align align))
                {
                    arrive.enabled = true;
                    align.enabled = true;
                    arrive.NewTarget(slots[i, j].virtualAgent);
                    align.NewTarget(slots[i, j].virtualAgent);
                }
            }
        }

        if (formationController != null)
            formationController.StartTimer();
    }

    public void LiberarAgents()
    {
        this.activated = false;
        
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc != null)
                {
                    DisableAllSteerings(slots[i, j].npc);
                    
                    if (!slots[i, j].leaderCell)
                    {
                        slots[i, j].npc = null;
                    }
                }
            }
        }

        DestroySlotVirtualAgents();
    }

    private void DestroySlotVirtualAgents()
    {
        if (virtualAgentsCleaned || slots == null) return;

        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                Agent virtualAgent = slots[i, j].virtualAgent;
                if (virtualAgent == null || virtualAgent.gameObject == null) continue;

                Destroy(virtualAgent.gameObject);
                slots[i, j].virtualAgent = null;
            }
        }

        virtualAgentsCleaned = true;
    }

    private void OnDestroy()
    {
        DestroySlotVirtualAgents();
    }

    public void LeaderWander()
    {
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc == null) continue;

                AgentNPC currentNPC = slots[i, j].npc;
                DisableAllSteerings(currentNPC);

                if (currentNPC == leader)
                {
                    if (currentNPC.TryGetComponent(out Wander wander)) wander.enabled = true;
                    if (leaderWallAvoidance && currentNPC.TryGetComponent(out WallAvoidance wall)) wall.enabled = true;
                }
                else
                {
                    ConfigureFollowerLeaderFollowing(currentNPC);
                }
            }
        }
    }

    public void StopLeaderWander()
    {
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                AgentNPC npc = slots[i, j].npc;
                if (npc == null) continue;
                
                DisableAllSteerings(npc);
                
                npc.Velocity = Vector3.zero;
                npc.Acceleration = Vector3.zero;
                npc.Rotation = 0f;
                npc.AngularAcc = 0f;
            }
        }
    }

    // --- REUBICACIÓN Y COLISIONES ---
    
    public void ReasignarCeldasOcupadas()
    {
        int totalReubicados = 0;
        List<string> detalleReubicaciones = debugReubicaciones ? new List<string>() : null;

        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc == null || EsCeldaLibre(i, j)) continue;

                AgentNPC npcAReasignar = slots[i, j].npc;
                bool esLider = slots[i, j].leaderCell;
                bool encontrado = false;

                for (int bi = 0; bi < numColumns && !encontrado; bi++)
                {
                    for (int bj = 0; bj < numRows && !encontrado; bj++)
                    {
                        if (slots[bi, bj].npc != null || !EsCeldaLibre(bi, bj)) continue;

                        slots[bi, bj].npc = npcAReasignar;
                        slots[bi, bj].leaderCell = esLider;
                        
                        if (esLider) leaderSlotRef = slots[bi, bj];

                        float orientacionDestino = slots[bi, bj].relativeOrientation;
                        slots[bi, bj].virtualAgent.UpdateVirtual(
                            slots[bi, bj].virtualAgent.Position,
                            ori: leaderAngle + orientacionDestino
                        );

                        slots[i, j].npc = null;
                        slots[i, j].leaderCell = false;

                        string quien = esLider ? "LÍDER" : npcAReasignar.name;
                        totalReubicados++;
                        if (debugReubicaciones)
                            detalleReubicaciones.Add($"{quien}: ({i},{j}) -> ({bi},{bj})");
                        encontrado = true;
                    }
                }

                if (!encontrado)
                {
                    string quien = esLider ? "LÍDER" : npcAReasignar.name;
                    Debug.LogWarning($"Sin celda libre para {quien}. Se queda en ({i},{j}) aunque esté bloqueada.");
                }
            }
        }

        if (debugReubicaciones && totalReubicados > 0)
        {
            Debug.LogWarning(
                $"Reubicaciones por obstaculos: {totalReubicados}\n" +
                string.Join("\n", detalleReubicaciones)
            );
        }
    }

    private bool EsCeldaLibre(int i, int j)
    {
        Vector3 pos = GridToPlane(i, j) + Vector3.up * 0.5f;
        Collider[] cols = Physics.OverlapSphere(pos, cellSize * 0.4f);
        foreach (Collider col in cols)
        {
            if (col.CompareTag("OCUPADO")) return false;
            AgentNPC npc = col.GetComponent<AgentNPC>();
            if (npc != null && !NpcEstaEnFormacion(npc)) return false;
        }
        return true;
    }

    private bool NpcEstaEnFormacion(AgentNPC npc)
    {
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc == npc) return true;
            }
        }
        return false;
    }

    public void OnDrawGizmos()
    {
        if (slots == null || !activated) return;

        Gizmos.color = Color.red;
        
        for (int i = 1; i < numColumns; i++)
        {
            Vector3 start = GridToPlane(i, 0) - new Vector3(cellSize / 2, 0, cellSize / 2);
            Vector3 end = GridToPlane(i, numRows - 1) + new Vector3(-cellSize / 2, 0, cellSize / 2);
            Gizmos.DrawLine(start, end);
        }

        for (int j = 1; j < numRows; j++)
        {
            Vector3 start = GridToPlane(0, j) - new Vector3(cellSize / 2, 0, cellSize / 2);
            Vector3 end = GridToPlane(numColumns - 1, j) + new Vector3(cellSize / 2, 0, -cellSize / 2);
            Gizmos.DrawLine(start, end);
        }

        bool first = true;
        Gizmos.color = Color.green;
        
        foreach (var slot in slots)
        {
            Gizmos.DrawSphere(slot.relativePosition + gridPosition, 0.3f);
            
            if (first)
            {
                first = false;
                Gizmos.color = Color.blue; 
            }
        }
    }
}