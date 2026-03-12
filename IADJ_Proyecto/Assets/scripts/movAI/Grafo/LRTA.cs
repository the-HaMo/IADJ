using System.Collections.Generic;
using UnityEngine;

public class LRTA : MonoBehaviour
{
    public enum HeuristicType
    {
        Manhattan,
        Chebyshev,
        Euclidean
    }

    [Header("Mapa")]
    [SerializeField] private Graph graph;

    [Header("Parametros")]
    [SerializeField] private HeuristicType heuristic = HeuristicType.Manhattan;

    [Header("Debbuging")]
    [SerializeField] private bool drawPath = true;
    [SerializeField] private Color pathColor = Color.cyan;
    [SerializeField] private bool drawLearnedCosts = true;
    [SerializeField] private Color learnedCostColor = Color.yellow;
    private List<Vector3> lastWorldPath = new List<Vector3>();
    private List<Vector3> activePath = new List<Vector3>();
    private int activePathIndex;
    private Agent virtualTarget;
    private AgentNPC ownerAgent;
    private Arrive arriveSteering;
    private List<Vector2Int> lastLss = new List<Vector2Int>();
    private const float CellCenterEpsilon = 0.05f;
    
    [Header("Guia de Steering")]
    [SerializeField] private Transform goal;

    [Header("LRTA* (Preparacion)")]
    [SerializeField] private bool initializeCostsOnPathRequest = true;
    [SerializeField] private bool recalculateEachCell = true;

    [Header("LRTA* LSS Config")]
    [SerializeField] private int maxLSSNodes = 20;
    private HeuristicaType heuristicaType;
    private readonly LssBuilder lssBuilder = new LssBuilder();
    private float[,] cellCosts;
    private Vector2Int costGoal;
    private bool costsInitialized;

    private void Awake()
    {
        if (graph == null)
        {
            graph = FindFirstObjectByType<Graph>();
        }

        BuildHeuristic();

        ownerAgent = GetComponent<AgentNPC>();
        arriveSteering = GetComponent<Arrive>();

        EnsureVirtualTarget();
        BindTargetToSteerings();
    }

    private void OnEnable()
    {
        EnsureVirtualTarget();
        BindTargetToSteerings();
    }

    private void OnDisable()
    {
        activePath.Clear();
        activePathIndex = 0;
    }

    private void OnDestroy()
    {
        if (virtualTarget != null && virtualTarget.gameObject != null)
        {
            Destroy(virtualTarget.gameObject);
        }
    }

    private void Update()
    {
        if (goal == null || ownerAgent == null)
        {
            return;
        }

        if (activePath.Count == 0 || activePathIndex >= activePath.Count)
        {
            RecalculateActivePath(ownerAgent.Position, goal.position);
        }

        if (activePath.Count == 0 || activePathIndex >= activePath.Count)
        {
            return;
        }

        Vector3 point = activePath[activePathIndex];
        float distance = PlanarDistance(ownerAgent.Position, point);
        bool reachedSameCell = IsSameGridCell(ownerAgent.Position, point);

        if (distance <= GetCenterArrivalThreshold() || reachedSameCell)
        {
            activePathIndex++;

            if (activePathIndex >= activePath.Count)
            {
                return; // Arrive frena naturalmente al llegar al objetivo
            }

            if (recalculateEachCell)
            {
                RecalculateActivePath(ownerAgent.Position, goal.position);
                if (activePath.Count == 0 || activePathIndex >= activePath.Count)
                {
                    return;
                }
            }

            point = activePath[activePathIndex];
        }

        EnsureVirtualTarget();
        point.y = ownerAgent.Position.y;
        virtualTarget.Position = point; // tener la misma altura 
    }

    public List<Vector3> GetLastWorldPath()
    {
        return new List<Vector3>(lastWorldPath);
    }

    public bool TryFindPathWorld(Vector3 startWorld, Vector3 goalWorld, out List<Vector3> worldPath)
    {
        worldPath = new List<Vector3>();
        lastWorldPath.Clear();

        if (graph == null)
        {
            return false;
        }

        graph.GetGridPosition(startWorld, out int startRow, out int startCol);
        graph.GetGridPosition(goalWorld, out int goalRow, out int goalCol);

        Vector2Int start = new Vector2Int(startRow, startCol);
        Vector2Int goal = new Vector2Int(goalRow, goalCol);

        if (!IsInsideGrid(start) || !IsInsideGrid(goal))
        {
            return false;
        }

        if (IsBlocked(start) || IsBlocked(goal))
        {
            return false;
        }

        if (initializeCostsOnPathRequest)
        {
            InitializeCellCosts(goal);
        }

        List<Vector2Int> pathGrid = LRTAStar(start, goal);

        if (pathGrid == null || pathGrid.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < pathGrid.Count; i++)
        {
            Vector2Int c = pathGrid[i];
            Vector3 corner = graph.GetWorldPosition(c.x, c.y);
            Vector3 center = corner + new Vector3(graph.tamCelda * 0.5f, 0f, graph.tamCelda * 0.5f);
            worldPath.Add(center);
        }

        lastWorldPath.AddRange(worldPath);
        return true;
    }

    public void InitializeCellCosts(Vector3 goalWorld)
    {
        if (graph == null)
        {
            return;
        }

        graph.GetGridPosition(goalWorld, out int goalRow, out int goalCol);
        InitializeCellCosts(new Vector2Int(goalRow, goalCol));
    }

    public void InitializeCellCosts(Vector2Int goalCoord)
    {
        if (graph == null || !IsInsideGrid(goalCoord))
        {
            return;
        }

        if (heuristicaType == null)
        {
            BuildHeuristic();
        }

        bool needsResize = cellCosts == null
            || cellCosts.GetLength(0) != graph.nFilas
            || cellCosts.GetLength(1) != graph.nColumnas;

        bool needsRebuild = !costsInitialized || needsResize || costGoal != goalCoord;
        if (!needsRebuild)
        {
            return;
        }

        if (needsResize)
        {
            cellCosts = new float[graph.nFilas, graph.nColumnas];
        }

        for (int row = 0; row < graph.nFilas; row++)
        {
            for (int col = 0; col < graph.nColumnas; col++)
            {
                Vector2Int cell = new Vector2Int(row, col);
                if (IsBlocked(cell))
                {
                    cellCosts[row, col] = float.PositiveInfinity;
                }
                else
                {
                    cellCosts[row, col] = heuristicaType.Evaluate(cell, goalCoord);
                }
            }
        }

        costGoal = goalCoord;
        costsInitialized = true;
    }

    public bool AreCostsInitialized()
    {
        return costsInitialized && cellCosts != null;
    }

    public float GetCellCost(Vector2Int coord)
    {
        if (!AreCostsInitialized() || !IsInsideGrid(coord))
        {
            return float.PositiveInfinity;
        }

        return cellCosts[coord.x, coord.y];
    }

    public void SetCellCost(Vector2Int coord, float newCost)
    {
        if (!AreCostsInitialized() || !IsInsideGrid(coord))
        {
            return;
        }

        if (coord == costGoal)
        {
            return;
        }

        cellCosts[coord.x, coord.y] = Mathf.Max(0f, newCost);
    }


private List<Vector2Int> LRTAStar(Vector2Int start, Vector2Int goal)
{
    // 1. Asegurar que los costes (h) están inicializados
    if (!AreCostsInitialized())
    {
        InitializeCellCosts(goal);
    }

    if (!AreCostsInitialized()) return null;

    // Si ya estamos en el objetivo, no hay camino que calcular
    if (start == goal) return new List<Vector2Int> { start };

    // --- PASO 1: LSS (Local Search Space) BFS ---
    List<Vector2Int> lss = lssBuilder.Build(start, maxLSSNodes, GetTraversableNeighbours);

    // --- PASO 2: APRENDIZAJE ---
    ValueUpdateStep(lss, goal);

    // --- PASO 3: SELECCIÓN DE ACCIÓN ---
    // Una vez aprendidos los nuevos costes, elegimos el mejor vecino
    Vector2Int nextMove = GetBestNeighbor(start);

    // Para LRTA* con steering, devolvemos el nodo actual y el siguiente.
    List<Vector2Int> path = new List<Vector2Int> { start, nextMove };
    lastLss = lss;

    return path;
}

    private bool IsInsideGrid(Vector2Int coord)
    {
        return coord.x >= 0 && coord.x < graph.nFilas && coord.y >= 0 && coord.y < graph.nColumnas;
    }

    private bool IsBlocked(Vector2Int coord)
    {
        Vector3 center = graph.GetWorldPosition(coord.x, coord.y) + new Vector3(graph.tamCelda * 0.5f, 1f, graph.tamCelda * 0.5f);
        Vector3 halfExtents = new Vector3(graph.tamCelda * 0.5f, 2f, graph.tamCelda * 0.5f);
        Collider[] collisions = Physics.OverlapBox(center, halfExtents);

        for (int i = 0; i < collisions.Length; i++)
        {
            if (collisions[i].CompareTag("OCUPADO"))
            {
                return true;
            }
        }

        return false;
    }

    private List<Vector2Int> GetTraversableNeighbours(Vector2Int current)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();

        for (int dRow = -1; dRow <= 1; dRow++)
        {
            for (int dCol = -1; dCol <= 1; dCol++)
            {
                if (dRow == 0 && dCol == 0)
                {
                    continue;
                }

                bool isDiagonal = dRow != 0 && dCol != 0;
                bool diagonalsEnabled = heuristicaType != null && heuristicaType.AllowsDiagonal;
                if (isDiagonal && !diagonalsEnabled)
                {
                    continue;
                }

                if (isDiagonal && IsCornerCutting(current, dRow, dCol))
                {
                    continue;
                }

                Vector2Int neighbourCoord = new Vector2Int(current.x + dRow, current.y + dCol);
                if (!IsInsideGrid(neighbourCoord) || IsBlocked(neighbourCoord))
                {
                    continue;
                }

                neighbours.Add(neighbourCoord);
            }
        }

        return neighbours;
    }

    private float MoveCost(Vector2Int current, Vector2Int neighbour)
    {
        bool diagonal = current.x != neighbour.x && current.y != neighbour.y;
        return diagonal ? 2f : 1f;
    }

    private bool IsCornerCutting(Vector2Int current, int dRow, int dCol)
    {
        Vector2Int sideA = new Vector2Int(current.x + dRow, current.y);
        Vector2Int sideB = new Vector2Int(current.x, current.y + dCol);

        if (!IsInsideGrid(sideA) || !IsInsideGrid(sideB))
        {
            return true;
        }

        return IsBlocked(sideA) || IsBlocked(sideB);
    }

    private void BuildHeuristic()
    {
        switch (heuristic)
        {
            case HeuristicType.Chebyshev:
                heuristicaType = new Chebyshev();
                break;
            case HeuristicType.Euclidean:
                heuristicaType = new Euclidean();
                break;
            default:
                heuristicaType = new Manhattan();
                break;
        }
    }

    private void EnsureVirtualTarget()
    {
        if (virtualTarget != null)
        {
            return;
        }

        float arrival = GetCenterArrivalThreshold();
        virtualTarget = Agent.CreateStaticVirtual(transform.position, 0.2f, arrival, 0f, false);
        virtualTarget.name = $"{name}_LRTATarget";
    }

    private void BindTargetToSteerings()
    {
        if (virtualTarget == null)
        {
            return;
        }

        if (arriveSteering != null)
        {
            arriveSteering.NewTarget(virtualTarget);
            arriveSteering.enabled = true;
        }

    }



    private void RecalculateActivePath(Vector3 startWorld, Vector3 goalWorld)
    {
        if (!TryFindPathWorld(startWorld, goalWorld, out List<Vector3> worldPath) || worldPath == null || worldPath.Count == 0)
        {
            activePath.Clear();
            activePathIndex = 0;
            return;
        }

        activePath.Clear();
        activePath.AddRange(worldPath);

        float arrival = GetCenterArrivalThreshold();
        if (activePath.Count > 0 && (PlanarDistance(startWorld, activePath[0]) <= arrival || IsSameGridCell(startWorld, activePath[0])))
        {
            activePathIndex = Mathf.Min(1, activePath.Count - 1);
        }
        else
        {
            activePathIndex = 0;
        }

        if (virtualTarget != null)
        {
            virtualTarget.ArrivalRadius = arrival;
            virtualTarget.InteriorRadius = Mathf.Min(0.15f, virtualTarget.ArrivalRadius);
        }
    }

    private float GetCenterArrivalThreshold()
    {
        if (graph == null)
        {
            return CellCenterEpsilon;
        }

        return Mathf.Max(CellCenterEpsilon, graph.tamCelda * 0.35f);
    }

    public void SetGoal(Transform newGoal)
    {
        goal = newGoal;
    }

    private float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private bool IsSameGridCell(Vector3 aWorld, Vector3 bWorld)
    {
        if (graph == null)
        {
            return false;
        }

        graph.GetGridPosition(aWorld, out int aRow, out int aCol);
        graph.GetGridPosition(bWorld, out int bRow, out int bCol);
        return aRow == bRow && aCol == bCol;
    }

    private void ValueUpdateStep(List<Vector2Int> lss, Vector2Int goal)
    {
        bool changed = true;
        // maxiteraciones = 100
        // iter = 0

        while (changed) // iter < maxiteraciones
        {
            changed = false;
            // iter++;
            foreach (Vector2Int u in lss)
            {
                if (u == goal) continue;

                float minExpected = float.PositiveInfinity;
                foreach (Vector2Int v in GetTraversableNeighbours(u))
                {
                    // f(v) = c(u,v) + h(v)
                    float fv = MoveCost(u, v) + GetCellCost(v);
                    if (fv < minExpected) minExpected = fv;
                }

                // Si el valor actual es menor que el mejor vecino, actualizamos (Bellman)
                if (minExpected > GetCellCost(u))
                {
                    SetCellCost(u, minExpected);
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
            // Coste de movimiento + Heurística aprendida
            float val = MoveCost(current, n) + GetCellCost(n);
            if (val < minVal)
            {
                minVal = val;
                best = n;
            }
        }
        return best;
    }

    private void OnDrawGizmos()
    {
        if (drawLearnedCosts)
        {
            DrawLearnedCostsGizmos();
        }

        if (lastLss != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Verde transparente
            foreach (var cell in lastLss)
            {
                Vector3 pos = graph.GetWorldPosition(cell.x, cell.y) + new Vector3(graph.tamCelda/2, 0.1f, graph.tamCelda/2);
                Gizmos.DrawCube(pos, new Vector3(graph.tamCelda, 0.1f, graph.tamCelda));
            }
        }

        if (!drawPath || lastWorldPath == null || lastWorldPath.Count < 2)
        {
            return;
        }

        Gizmos.color = pathColor;
        for (int i = 0; i < lastWorldPath.Count - 1; i++)
        {
            Gizmos.DrawLine(lastWorldPath[i] + Vector3.up * 0.1f, lastWorldPath[i + 1] + Vector3.up * 0.1f);
        }
    }

    private void DrawLearnedCostsGizmos()
    {
        if (!costsInitialized || cellCosts == null || graph == null)
        {
            return;
        }

        float halfCell = graph.tamCelda * 0.5f;
        for (int row = 0; row < graph.nFilas; row++)
        {
            for (int col = 0; col < graph.nColumnas; col++)
            {
                float cost = cellCosts[row, col];
                if (float.IsInfinity(cost))
                {
                    continue;
                }

                Vector3 corner = graph.GetWorldPosition(row, col);
                Vector3 center = corner + new Vector3(halfCell, 0.05f, halfCell);
                Gizmos.color = learnedCostColor;
                Gizmos.DrawWireCube(center, new Vector3(graph.tamCelda * 0.15f, 0.02f, graph.tamCelda * 0.15f));

                #if UNITY_EDITOR
                GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
                style.normal.textColor = learnedCostColor;
                string label = cost.ToString("0.00");
                UnityEditor.Handles.Label(center + Vector3.up * 0.05f, label, style);
                #endif
            }
        }
    }

}
