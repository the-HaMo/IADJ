using System.Collections.Generic;
using UnityEngine;

public class LRTA : MonoBehaviour
{
    public enum HeuristicType { Manhattan, Chebyshev, Euclidean }

    [Header("Referencias")]
    [SerializeField] private Graph graph;
    [SerializeField] private Transform goal;

    [Header("Parámetros LRTA*")]
    [SerializeField] private HeuristicType heuristic = HeuristicType.Manhattan;
    [SerializeField] private int maxLSSNodes = 20;
    [SerializeField] private bool recalculateEachCell = true; // Si true, recalcula el camino cada vez que se alcanza una nueva celda del camino activo

    [Header("Depuración (Gizmos)")]
    [SerializeField] private bool drawPath = true;
    [SerializeField] private Color pathColor = Color.cyan;
    [SerializeField] private bool drawLearnedCosts = true;
    [SerializeField] private Color learnedCostColor = Color.yellow;

    private AgentNPC ownerAgent;
    private Arrive arriveSteering;
    private HeuristicaType heuristica;
    private float[,] cellCosts;
    private Vector2Int costGoal;
    private bool costsInitialized;

    // Estado del pathfinding
    private List<Vector3> activePath = new List<Vector3>();
    private List<Vector3> lastWorldPath = new List<Vector3>();
    private List<Vector2Int> lastLss = new List<Vector2Int>();
    private int activePathIndex;

    // Elementos de Steering devueltos
    private Agent virtualTarget;
    private const float CellCenterEpsilon = 0.5f;

    private void Awake()
    {
        graph = graph != null ? graph : FindFirstObjectByType<Graph>();
        ownerAgent = GetComponent<AgentNPC>();
        arriveSteering = GetComponent<Arrive>();
        BuildHeuristic();
        
        EnsureVirtualTarget();
        BindTargetToSteerings();
    }

    // Limpiar el estado al desactivar el componente para evitar que el agente siga un camino obsoleto al reactivar
    private void OnEnable()
    {
        EnsureVirtualTarget();
        BindTargetToSteerings();
    }

    // Limpiar el estado al desactivar el componente para evitar que el agente siga un camino obsoleto al reactivar
    private void OnDisable()
    {
        activePath.Clear();
        activePathIndex = 0;
    }

    // Limpiar el target virtual al destruir el objeto para evitar objetos huérfanos en la escena
    private void OnDestroy()
    {
        if (virtualTarget != null && virtualTarget.gameObject != null)
        {
            Destroy(virtualTarget.gameObject);
        }
    }

    private void Update()
    {
        if (goal == null || ownerAgent == null) return;

        bool needsRecalculation = activePath.Count == 0 || activePathIndex >= activePath.Count;
        
        if (!needsRecalculation)
        {
            Vector3 targetPoint = activePath[activePathIndex];
            if (IsSameGridCell(ownerAgent.Position, targetPoint))
            {
                activePathIndex++;
                if (recalculateEachCell) needsRecalculation = true;
            }
        }

        if (needsRecalculation)
        {
            if (TryFindPathWorld(ownerAgent.Position, goal.position, out List<Vector3> newPath))
            {
                activePath = newPath;
                activePathIndex = 1; // 0 es la posición actual, 1 es el siguiente paso
            }
        }

        // Actualizar la posición del target virtual si hay un camino válido
        if (activePathIndex < activePath.Count && virtualTarget != null)
        {
            Vector3 point = activePath[activePathIndex];
            point.y = ownerAgent.Position.y; // Mantener la misma altura
            virtualTarget.Position = point; 
        }
    }

    // --- TRADUCCIÓN MUNDO 3D a pos del grafo ---
    public bool TryFindPathWorld(Vector3 startWorld, Vector3 goalWorld, out List<Vector3> worldPath)
    {
        worldPath = new List<Vector3>();
        lastWorldPath.Clear();

        graph.GetGridPosition(startWorld, out int startRow, out int startCol);
        graph.GetGridPosition(goalWorld, out int goalRow, out int goalCol);

        Vector2Int start = new Vector2Int(startRow, startCol);
        Vector2Int goal = new Vector2Int(goalRow, goalCol);

        if (!IsInsideGrid(start) || !IsInsideGrid(goal) || IsBlocked(goal)) return false;

        InitializeCellCosts(goal);

        List<Vector2Int> pathGrid = LRTAStar(start, goal);

        if (pathGrid == null || pathGrid.Count == 0) return false;

        foreach (var cell in pathGrid)
        {
            worldPath.Add(GetCellCenter(cell));
        }

        lastWorldPath.AddRange(worldPath);
        return true;
    }

    // --- (LRTA*) ---
    private List<Vector2Int> LRTAStar(Vector2Int start, Vector2Int goal)
    {
        if (start == goal) return new List<Vector2Int> { start };

        lastLss = new LssBuilder().Build(start, maxLSSNodes, GetTraversableNeighbours);
        ValueUpdateStep(lastLss, goal);
        Vector2Int nextMove = GetBestNeighbor(start);

        return new List<Vector2Int> { start, nextMove };
    }

    // --- MÉTODOS DE SOPORTE Y VIRTUAL TARGET ---
    
    // Asegura que el target virtual exista y esté configurado correctamente. Si no existe, lo crea.
    private void EnsureVirtualTarget()
    {
        if (virtualTarget != null) return;

        float arrival = Mathf.Max(CellCenterEpsilon, graph.tamCelda * 0.5f);
        virtualTarget = Agent.CreateStaticVirtual(transform.position, 0.2f, arrival, 0f, false);
        virtualTarget.name = $"{name}_LRTATarget";
    }

    private void BindTargetToSteerings()
    {
        if (virtualTarget == null || arriveSteering == null) return;
        
        arriveSteering.NewTarget(virtualTarget);
        arriveSteering.enabled = true;
    }

    private void InitializeCellCosts(Vector2Int goalCoord)
    {
        if (costsInitialized && costGoal == goalCoord) return;

        cellCosts = cellCosts ?? new float[graph.nFilas, graph.nColumnas];
        for (int row = 0; row < graph.nFilas; row++)
        {
            for (int col = 0; col < graph.nColumnas; col++)
            {
                Vector2Int cell = new Vector2Int(row, col);
                cellCosts[row, col] = IsBlocked(cell) ? float.PositiveInfinity : heuristica.Evaluate(cell, goalCoord);
            }
        }

        costGoal = goalCoord;
        costsInitialized = true;
    }

    private void ValueUpdateStep(List<Vector2Int> lss, Vector2Int goalCoord)
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (Vector2Int u in lss)
            {
                if (u == goalCoord) continue;

                float minExpected = float.PositiveInfinity;
                foreach (Vector2Int v in GetTraversableNeighbours(u))
                {
                    float fv = MoveCost(u, v) + GetCellCost(v);
                    if (fv < minExpected) minExpected = fv;
                }

                if (minExpected > GetCellCost(u))
                {
                    cellCosts[u.x, u.y] = minExpected;
                    changed = true;
                }
            }
        }
    }

    private Vector2Int GetBestNeighbor(Vector2Int current)
    {
        List<Vector2Int> neighbors = GetTraversableNeighbours(current);
        if (neighbors.Count == 0) return current;

        Vector2Int best = neighbors[0];
        float minVal = float.PositiveInfinity;

        foreach (Vector2Int n in neighbors)
        {
            float val = MoveCost(current, n) + GetCellCost(n);
            if (val < minVal)
            {
                minVal = val;
                best = n;
            }
        }
        return best;
    }

    private List<Vector2Int> GetTraversableNeighbours(Vector2Int current)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();
        for (int dRow = -1; dRow <= 1; dRow++)
        {
            for (int dCol = -1; dCol <= 1; dCol++)
            {
                if (dRow == 0 && dCol == 0) continue;

                bool isDiagonal = dRow != 0 && dCol != 0;
                if (isDiagonal && (!heuristica.AllowsDiagonal || IsCornerCutting(current, dRow, dCol))) continue;

                Vector2Int neighbour = new Vector2Int(current.x + dRow, current.y + dCol);
                if (IsInsideGrid(neighbour) && !IsBlocked(neighbour))
                {
                    neighbours.Add(neighbour);
                }
            }
        }
        return neighbours;
    }

    // --- UTILS ---
    private float GetCellCost(Vector2Int c) => IsInsideGrid(c) ? cellCosts[c.x, c.y] : float.PositiveInfinity;
    private bool IsInsideGrid(Vector2Int c) => c.x >= 0 && c.x < graph.nFilas && c.y >= 0 && c.y < graph.nColumnas;
    private bool IsBlocked(Vector2Int c) => graph.isOcupada(c.x, c.y);
    private float MoveCost(Vector2Int a, Vector2Int b) => (a.x != b.x && a.y != b.y) ? 2f : 1f;
    private bool IsSameGridCell(Vector3 a, Vector3 b)
    {
        graph.GetGridPosition(a, out int aR, out int aC);
        graph.GetGridPosition(b, out int bR, out int bC);
        return aR == bR && aC == bC;
    }
    private bool IsCornerCutting(Vector2Int c, int dRow, int dCol) => IsBlocked(new Vector2Int(c.x + dRow, c.y)) || IsBlocked(new Vector2Int(c.x, c.y + dCol));
    private Vector3 GetCellCenter(Vector2Int c) => graph.GetWorldPosition(c.x, c.y) + new Vector3(graph.tamCelda * 0.5f, 0, graph.tamCelda * 0.5f);

    private void BuildHeuristic()
    {
        heuristica = heuristic switch
        {
            HeuristicType.Chebyshev => new Chebyshev(),
            HeuristicType.Euclidean => new Euclidean(),
            _ => new Manhattan()
        };
    }

    // --- GIZMOS ---
    private void OnDrawGizmos()
    {
        if (drawLearnedCosts && costsInitialized && cellCosts != null && graph != null)
        {
            float halfCell = graph.tamCelda * 0.5f;
            for (int row = 0; row < graph.nFilas; row++)
            {
                for (int col = 0; col < graph.nColumnas; col++)
                {
                    float cost = cellCosts[row, col];
                    if (float.IsInfinity(cost)) continue;

                    Vector3 center = graph.GetWorldPosition(row, col) + new Vector3(halfCell, 0.05f, halfCell);
                    Gizmos.color = learnedCostColor;
                    Gizmos.DrawWireCube(center, new Vector3(graph.tamCelda * 0.15f, 0.02f, graph.tamCelda * 0.15f));

#if UNITY_EDITOR
                    GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { normal = { textColor = learnedCostColor } };
                    UnityEditor.Handles.Label(center + Vector3.up * 0.05f, cost.ToString("0"), style);
#endif
                }
            }
        }

        if (lastLss != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            foreach (var cell in lastLss)
            {
                Vector3 pos = GetCellCenter(cell);
                Gizmos.DrawCube(pos, new Vector3(graph.tamCelda, 0.1f, graph.tamCelda));
            }
        }

        if (drawPath && lastWorldPath != null && lastWorldPath.Count >= 2)
        {
            Gizmos.color = pathColor;
            for (int i = 0; i < lastWorldPath.Count - 1; i++)
            {
                Gizmos.DrawLine(lastWorldPath[i] + Vector3.up * 0.1f, lastWorldPath[i + 1] + Vector3.up * 0.1f);
            }
        }
    }
}