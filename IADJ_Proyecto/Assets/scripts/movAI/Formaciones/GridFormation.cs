using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Estructura que representa un slot (ranura) en la formación
/// </summary>
public struct Slot
{
    // NPC asociado a la celda
    public AgentNPC npc;

    // Posición con respecto al líder
    public Vector3 relativePosition;

    // Orientación con respecto al líder
    public float relativeOrientation;

    // Indica si esta celda es la del líder
    public bool leaderCell;

    // Agente virtual que usarán los NPCs para posicionarse como deben en la celda
    public Agent virtualAgent;
}

/// <summary>
/// Grid de formación que administra los slots y posiciones de los agentes.
/// Implementa el sistema de Leader Following del apartado h).
/// </summary>
public class GridFormation : MonoBehaviour
{
    private const float FOLLOWER_FLEE_DISTANCE = 1.2f;
    [SerializeField] private bool debugReubicaciones = true;

    // Columnas del grid
    public int numColumns;
    
    // Filas del grid
    public int numRows;
    
    // Tamaño de celda
    public float cellSize;
    
    // Agente líder (una vez asignado SIEMPRE será el mismo)
    public AgentNPC leader;

    // Orientación real del líder (su orientación relativa será 0)
    public float leaderAngle;

    // Celdas del grid
    public Slot[,] slots; // Matriz de ranuras

    // Posición real del grid (posición de la celda del líder)
    public Vector3 gridPosition;

    // Manejador del grid
    public FormationController formationController;

    // Usado para llevar el grid a la posición del líder al hacer F
    public bool activated = false;

    /// <summary>
    /// "Constructor" del grid. Como GridFormation hereda de MonoBehaviour no permite constructores.
    /// Se usa justo después del AddComponent para preparar el grid.
    /// </summary>
    public void CreateGridManager(float cellSize, AgentNPC leader, int leaderI, int leaderJ, float angle, int numColumns, int numRows)
    {
        this.activated = true;
        this.numColumns = numColumns;
        this.numRows = numRows;
        this.slots = new Slot[numColumns, numRows];
        this.cellSize = cellSize;
        this.leader = leader;
        this.leaderAngle = angle;
        this.gridPosition = leader.Position;
        this.formationController = FindFirstObjectByType<FormationController>();

        // Para cada celda del grid
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                this.slots[i, j] = new Slot();
                
                // Calculamos la posición relativa desde la celda del líder hasta esta celda
                this.slots[i, j].relativePosition = new Vector3(
                    i * cellSize - leaderI * cellSize, 
                    0f, 
                    j * cellSize - leaderJ * cellSize
                );

                // Creamos un virtualAgent en la posición real del mundo donde se encuentra la celda
                this.slots[i, j].virtualAgent = Agent.CreateStaticVirtual(
                    GridToPlane(i, j), 
                    intRadius: 0.5f, 
                    arrRadius: 2f,
                    ori: angle, 
                    paint: false
                );

                // Las orientaciones se especificarán más adelante
                this.slots[i, j].relativeOrientation = 0f;

                // Si esta celda es la celda del líder
                if (leaderI == i && leaderJ == j)
                {
                    this.slots[i, j].npc = this.leader;
                    this.slots[i, j].leaderCell = true;
                }
                else
                {
                    this.slots[i, j].npc = null;
                    this.slots[i, j].leaderCell = false;
                }
            }
        }
    }

    /// <summary>
    /// Conecta un agente a su celda
    /// </summary>S
    public void LinkToSlot(int i, int j, float angle, AgentNPC npc)
    {
        this.slots[i, j].npc = npc;
        
        // La orientación relativa es la orientación respecto al líder
        this.slots[i, j].relativeOrientation = angle;
        
        Agent v = this.slots[i, j].virtualAgent;
        
        // Actualizar el agente virtual con la orientación necesaria
        v.UpdateVirtual(v.Position, ori: leaderAngle + angle);
    }

    /// <summary>
    /// Dada una posición relativa, devuelve la posición de la ranura en el mundo real
    /// </summary>
    public Vector3 GridToPlane(int i, int j)
    {
        // Convertir el ángulo del líder a radianes
        float cosAngle = Mathf.Cos(leaderAngle * Mathf.Deg2Rad);
        float sinAngle = Mathf.Sin(leaderAngle * Mathf.Deg2Rad);

        // Aplica matriz de rotación 2D manual (plano XZ)
        Vector3 relativeLoc = slots[i, j].relativePosition;
        float rotatedX = relativeLoc.x * cosAngle - relativeLoc.z * sinAngle;
        float rotatedZ = relativeLoc.x * sinAngle + relativeLoc.z * cosAngle;

        // Transforma la posición relativa a coordenadas globales
        return gridPosition + new Vector3(rotatedX, 0, rotatedZ);
    }

    /// <summary>
    /// Mueve el grid a una nueva posición
    /// </summary>
    public void MoveGrid(Vector3 newPosition)
    {
        this.gridPosition = newPosition;
        
        // 1. ANTES de mover, recuperamos la lista de seguidores actuales.
        // Luego reconstruiremos la asignacion desde el patron para evitar
        // arrastrar la celda reasignada del movimiento anterior.
        List<AgentNPC> listaTemporalNPCs = new List<AgentNPC>();

        // Guardar seguidores actuales (no lider)
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc != null && slots[i, j].npc != leader)
                {
                    listaTemporalNPCs.Add(slots[i, j].npc);
                }

                // Limpiar todas las asignaciones previas para reconstruir desde patron
                slots[i, j].npc = null;
                slots[i, j].leaderCell = false;
            }
        }

        // 2. Restaurar asignacion base del patron (incluido lider en su slot original)
        if (formationController != null)
        {
            FormationPattern activePattern = formationController.GetPattern();
            (int leaderI, int leaderJ) = activePattern.GetLeaderSlot();

            slots[leaderI, leaderJ].npc = leader;
            slots[leaderI, leaderJ].leaderCell = true;
            slots[leaderI, leaderJ].relativeOrientation = activePattern.GetAngle(0);

            // Re-asignamos los seguidores en los slots válidos del patrón original
            var validSlots = activePattern.GetValidSlots();
            for (int k = 0; k < listaTemporalNPCs.Count && k < validSlots.Length; k++)
            {
                int f = validSlots[k].Item1;
                int c = validSlots[k].Item2;
                slots[f, c].npc = listaTemporalNPCs[k];
                // Importante: restaurar el ángulo relativo original del slot del patrón
                slots[f, c].relativeOrientation = activePattern.GetAngle(k + 1);
            }
        }

        // 3. Ahora actualizamos las posiciones de los virtual agents como antes
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                this.slots[i, j].virtualAgent.Position = GridToPlane(i, j);
                this.slots[i, j].virtualAgent.Orientation = leaderAngle + this.slots[i, j].relativeOrientation;
            }
        }

        // 4. Finalmente, si el nuevo punto TIENE obstáculos, la reasignación actuará DE NUEVO
        // pero partiendo de la formación ideal, no de la formación "rota" anterior.
        ReasignarCeldasOcupadas();
    }
    /// <summary>
    /// Devuelve la celda del líder
    /// </summary>
    public Slot GetLeaderSlot()
    {
        // Prioridad: la celda donde realmente esta el objeto lider.
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc == leader)
                    return slots[i, j];
            }
        }

        // Fallback por flag (si hubo un estado intermedio sin referencia directa).
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].leaderCell) 
                    return slots[i, j];
            }
        }

        // Ultimo fallback seguro.
        return slots[0, 0];
    }

    /// <summary>
    /// Implementación del Leader Following.
    /// Cuando se mueve el grid, el líder va al destino y los demás lo siguen.
    /// </summary>
    public void LeaderFollowing()
    {
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc != null)
                {
                    Agent leaderVirtual = GetLeaderSlot().virtualAgent;
                    AgentNPC currentNPC = slots[i, j].npc;

                    // Obtener componentes de steering
                    Arrive arrive = currentNPC.GetComponent<Arrive>();
                    Face face = currentNPC.GetComponent<Face>();
                    Align align = currentNPC.GetComponent<Align>();
                    Wander wander = currentNPC.GetComponent<Wander>();

                    if (arrive == null || face == null)
                    {
                        Debug.LogWarning($"NPC {currentNPC.name} no tiene Arrive o Face");
                        continue;
                    }

                    // Si es el líder, va hacia donde está el nuevo grid
                    if (slots[i, j].npc == leader)
                    {
                        Wander w = currentNPC.GetComponent<Wander>();
                        Flee flee = currentNPC.GetComponent<Flee>();
                        Separation separation = currentNPC.GetComponent<Separation>();
                        if (w != null) w.enabled = false;
                        if (flee != null) flee.enabled = false;
                        if (separation != null) separation.enabled = false;
                        if (align != null) align.enabled = false;
                        arrive.enabled = true;
                        face.enabled = true;
                        arrive.NewTarget(leaderVirtual);
                        face.NewTarget(leaderVirtual);
                    }
                    // Si es seguidor, debe ir a SU slot del patron.
                    // Durante el desplazamiento la formacion se rompe:
                    // siguen al lider con Arrive + Separation + Flee (condicional por cercania).
                    else
                    {
                        ConfigureFollowerLeaderFollowing(currentNPC);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Cuando el líder ha llegado a la nueva posición del grid.
    /// Esta función hace que cada NPC vaya a su celda correspondiente y se orienten.
    /// Antes de enviarlos, vuelve a chequear celdas bloqueadas (el estado puede haber
    /// cambiado desde que se llamó a MoveGrid/Formar).
    /// </summary>
    public void AgentsToCell()
    {
        // Rechequear celdas bloqueadas justo antes de que los NPCs partan a sus destinos.
        // Esto cubre el caso en que un objeto con tag OCUPADO esté exactamente en la posición
        // final de la celda, no en la posición inicial cuando se hizo F o clic derecho.
        ReasignarCeldasOcupadas();

        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc == null) continue;

                AgentNPC currentNPC = slots[i, j].npc;

                // Obtener componentes
                Arrive arrive = currentNPC.GetComponent<Arrive>();
                Align align   = currentNPC.GetComponent<Align>();

                if (arrive != null && align != null)
                {
                    // En formación solo deben influir Arrive + Align.
                    SteeringBehaviour[] allSteerings = currentNPC.GetComponents<SteeringBehaviour>();
                    foreach (SteeringBehaviour sb in allSteerings)
                    {
                        if (sb == null) continue;
                        sb.enabled = false;
                    }

                    arrive.enabled = true;
                    align.enabled  = true;

                    // Usamos el virtualAgent de la celda que tiene asignada el NPC
                    // DESPUÉS de la reasignación (slots[i,j] ya es la celda correcta).
                    arrive.NewTarget(slots[i, j].virtualAgent);
                    align.NewTarget(slots[i, j].virtualAgent);
                }
                else
                {
                    Debug.LogWarning($"NPC {currentNPC.name} no tiene Arrive o Align necesarios para ir a celda.");
                }
            }
        }

        if (formationController != null)
            formationController.StartTimer();
    }

    /// <summary>
    /// Libera todos los agentes de la formación (excepto el líder)
    /// </summary>
    public void LiberarAgents()
    {
        this.activated = false;
        
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc != null && !slots[i, j].leaderCell)
                {
                    // Desactivar steering de formación
                    Arrive arrive = slots[i, j].npc.GetComponent<Arrive>();
                    Align align = slots[i, j].npc.GetComponent<Align>();
                    Face face = slots[i, j].npc.GetComponent<Face>();
                    Wander wander = slots[i, j].npc.GetComponent<Wander>();
                    Flee flee = slots[i, j].npc.GetComponent<Flee>();
                    Separation separation = slots[i, j].npc.GetComponent<Separation>();
                    
                    if (arrive != null) arrive.enabled = false;
                    if (align != null) align.enabled = false;
                    if (face != null) face.enabled = false;
                    if (wander != null) wander.enabled = false;
                    if (flee != null) flee.enabled = false;
                    if (separation != null) separation.enabled = false;
                    
                    slots[i, j].npc = null;
                }
                
                // Si es el líder, solo desactivar steering
                if (slots[i, j].npc == leader)
                {
                    Arrive arrive = leader.GetComponent<Arrive>();
                    Align align = leader.GetComponent<Align>();
                    Face face = leader.GetComponent<Face>();
                    Wander wander = leader.GetComponent<Wander>();
                    Flee flee = leader.GetComponent<Flee>();
                    Separation separation = leader.GetComponent<Separation>();
                    
                    if (arrive != null) arrive.enabled = false;
                    if (align != null) align.enabled = false;
                    if (face != null) face.enabled = false;
                    if (wander != null) wander.enabled = false;
                    if (flee != null) flee.enabled = false;
                    if (separation != null) separation.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// Cuando la formación está parada, el líder entra en Wander y los demás lo siguen
    /// </summary>
    public void LeaderWander()
    {
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc != null)
                {
                    if (slots[i, j].npc == leader)
                    {
                        // El líder activa Wander
                        Wander wander = leader.GetComponent<Wander>();
                        Arrive arrive = leader.GetComponent<Arrive>();
                        Align align = leader.GetComponent<Align>();
                        Face face = leader.GetComponent<Face>();
                        Flee flee = leader.GetComponent<Flee>();
                        Separation separation = leader.GetComponent<Separation>();
                        WallAvoidance wallAvoidance = leader.GetComponent<WallAvoidance>();
                        if (arrive != null) arrive.enabled = false;
                        if (align != null) align.enabled = false;
                        if (face != null) face.enabled = false;
                        if (flee != null) flee.enabled = false;
                        if (separation != null) separation.enabled = false;
                        if (wander != null) wander.enabled = true;
                        else Debug.LogWarning($"El líder {leader.name} no tiene Wander en el Inspector.");
                        if (wallAvoidance != null) wallAvoidance.enabled = true;
                        else Debug.LogWarning($"El líder {leader.name} no tiene WallAvoidance en el Inspector.");
                    }
                    else
                    {
                        // Los seguidores: Seek al líder (simple y directo)
                        // El líder ya evita paredes; los seguidores solo lo persiguen.
                        AgentNPC follower = slots[i, j].npc;

                        Seek seek           = follower.GetComponent<Seek>();
                        WallAvoidance wall  = follower.GetComponent<WallAvoidance>();
                        Arrive arrive       = follower.GetComponent<Arrive>();
                        Align align         = follower.GetComponent<Align>();
                        Face face           = follower.GetComponent<Face>();
                        Flee flee           = follower.GetComponent<Flee>();
                        Wander wander       = follower.GetComponent<Wander>();

                        // Apagar todo lo que no usamos
                        if (arrive != null) arrive.enabled = false;
                        if (align != null)  align.enabled = false;
                        if (face != null)   face.enabled = false;
                        if (flee != null)   flee.enabled = false;
                        if (wander != null) wander.enabled = false;

                        if (seek != null)
                        {
                            seek.enabled = true;
                            seek.NewTarget(leader);
                        }
                        else
                        {
                            // Fallback robusto: si falta Seek, seguir al líder con la
                            // configuración de Leader Following (Arrive + Separation + Flee condicional).
                            ConfigureFollowerLeaderFollowing(follower);
                        }

                        if (wall != null)  wall.enabled = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Detiene el modo wander-follow actual: apaga steerings de ese modo y frena a todos.
    /// </summary>
    public void StopLeaderWander()
    {
        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                AgentNPC npc = slots[i, j].npc;
                if (npc == null) continue;

                Arrive arrive = npc.GetComponent<Arrive>();
                Align align = npc.GetComponent<Align>();
                Face face = npc.GetComponent<Face>();
                Seek seek = npc.GetComponent<Seek>();
                Wander wander = npc.GetComponent<Wander>();
                Flee flee = npc.GetComponent<Flee>();
                Separation separation = npc.GetComponent<Separation>();
                WallAvoidance wallAvoidance = npc.GetComponent<WallAvoidance>();

                if (arrive != null) arrive.enabled = false;
                if (align != null) align.enabled = false;
                if (face != null) face.enabled = false;
                if (seek != null) seek.enabled = false;
                if (wander != null) wander.enabled = false;
                if (flee != null) flee.enabled = false;
                if (separation != null) separation.enabled = false;
                if (wallAvoidance != null) wallAvoidance.enabled = false;

                // Frenar al instante para marcar la fase de pausa del bucle.
                npc.Velocity = Vector3.zero;
                npc.Acceleration = Vector3.zero;
                npc.Rotation = 0f;
                npc.AngularAcc = 0f;
            }
        }
    }


    private void ConfigureFollowerLeaderFollowing(AgentNPC follower)
    {
        Arrive arrive = follower.GetComponent<Arrive>();
        Face face = follower.GetComponent<Face>();
        Align align = follower.GetComponent<Align>();
        Wander wander = follower.GetComponent<Wander>();
        Seek seek = follower.GetComponent<Seek>();

        if (arrive == null || face == null)
        {
            return;
        }

        Flee flee = follower.GetComponent<Flee>();
        Separation separation = follower.GetComponent<Separation>();

        if (align != null) align.enabled = false;
        if (wander != null) wander.enabled = false;
        if (seek != null) seek.enabled = false;

        arrive.enabled = true;
        face.enabled = true;
        arrive.NewTarget(leader);
        face.NewTarget(leader);

        if (separation != null) separation.enabled = true;

        if (flee != null)
        {
            float distanceToLeader = Vector3.Distance(follower.Position, leader.Position);
            if (distanceToLeader < FOLLOWER_FLEE_DISTANCE)
            {
                flee.enabled = true;
                flee.NewTarget(leader);
            }
            else
            {
                flee.enabled = false;
            }
        }
    }

    /// <summary>
    /// Cada NPC (incluido el líder) comprueba si su celda de destino está libre
    /// (tag OCUPADO u obstáculo). Si está bloqueada, se reasigna a la primera celda
    /// libre encontrada. Para el líder, el flag leaderCell se transfiere también a la
    /// nueva celda para que el sistema siga reconociéndolo como líder.
    /// </summary>
    public void ReasignarCeldasOcupadas()
    {
        int totalReubicados = 0;
        List<string> detalleReubicaciones = debugReubicaciones ? new List<string>() : null;

        for (int i = 0; i < numColumns; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                if (slots[i, j].npc == null) continue;   // celda vacía → ignorar
                if (EsCeldaLibre(i, j)) continue;         // celda libre → nada que hacer

                // La celda está bloqueada → buscar la primera celda libre
                AgentNPC npcAReasignar     = slots[i, j].npc;
                bool esLider               = slots[i, j].leaderCell;
                bool encontrado            = false;

                for (int bi = 0; bi < numColumns && !encontrado; bi++)
                {
                    for (int bj = 0; bj < numRows && !encontrado; bj++)
                    {
                        // La celda destino debe estar vacía y sin obstáculo
                        if (slots[bi, bj].npc != null) continue;
                        if (!EsCeldaLibre(bi, bj)) continue;

                        // Reasignar el NPC a la nueva celda
                        slots[bi, bj].npc                 = npcAReasignar;
                        // Si es el líder, transferir también el flag leaderCell
                        slots[bi, bj].leaderCell          = esLider;

                        // Mantener orientacion de la celda destino (patron),
                        // para que la formacion no arrastre orientaciones anteriores.
                        float orientacionDestino = slots[bi, bj].relativeOrientation;

                        // Actualizar el virtualAgent de la nueva celda con la orientación correcta
                        slots[bi, bj].virtualAgent.UpdateVirtual(
                            slots[bi, bj].virtualAgent.Position,
                            ori: leaderAngle + orientacionDestino
                        );

                        // Limpiar la celda original
                        slots[i, j].npc        = null;
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

    /// <summary>
    /// Devuelve true si la celda (i,j) no tiene tag OCUPADO ni un NPC ajeno a la formación.
    /// </summary>
    private bool EsCeldaLibre(int i, int j)
    {
        // Elevamos el punto ligeramente sobre el suelo para no detectar el terreno
        Vector3 pos = GridToPlane(i, j) + Vector3.up * 0.5f;
        Collider[] cols = Physics.OverlapSphere(pos, cellSize * 0.9f);
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
        foreach (Slot s in slots)
            if (s.npc == npc) return true;
        return false;
    }

    /// <summary>
    /// Dibuja el grid en la escena para debug
    /// </summary>
    public void OnDrawGizmos()
    {
        if (slots == null || !activated) return;

        Gizmos.color = Color.red;
        
        // Dibujar líneas verticales
        for (int i = 1; i < numColumns; i++)
        {
            Vector3 start = GridToPlane(i, 0) - new Vector3(cellSize / 2, 0, cellSize / 2);
            Vector3 end = GridToPlane(i, numRows - 1) + new Vector3(-cellSize / 2, 0, cellSize / 2);
            Gizmos.DrawLine(start, end);
        }

        // Dibujar líneas horizontales
        for (int j = 1; j < numRows; j++)
        {
            Vector3 start = GridToPlane(0, j) - new Vector3(cellSize / 2, 0, cellSize / 2);
            Vector3 end = GridToPlane(numColumns - 1, j) + new Vector3(cellSize / 2, 0, -cellSize / 2);
            Gizmos.DrawLine(start, end);
        }

        // Dibujar posiciones de slots
        bool first = true;
        Gizmos.color = Color.green;
        
        foreach (var slot in slots)
        {
            Gizmos.DrawSphere(slot.relativePosition + gridPosition, 0.3f);
            
            if (first)
            {
                first = false;
                Gizmos.color = Color.blue; // El líder en azul
            }
        }
    }
}
