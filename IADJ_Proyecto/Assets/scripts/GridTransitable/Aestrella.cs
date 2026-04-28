using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public enum HeuristicType { Manhattan, Euclidean, Chebyshev }
    
    [Header("Configuración A*")]
    public HeuristicType selectedHeuristic = HeuristicType.Manhattan;



    [Header("Debug Visual")]
    public bool mostrarCaminosEnEscena = false;

    private GridManager gridManager;
    private HeuristicaType heuristicaInstancia;
    private MapaInfluencia mapaInfluencia;

    void Awake()
    {
        gridManager = GetComponent<GridManager>();
        InicializarHeuristica();
    }

    void Start()
    {
        mapaInfluencia = FindFirstObjectByType<MapaInfluencia>();
    }

    void Update()
    {
    }

    // Se ejecuta automáticamente al cambiar valores en el Inspector
    private void OnValidate()
    {
        InicializarHeuristica();
    }

    private void InicializarHeuristica()
    {
        switch (selectedHeuristic)
        {
            case HeuristicType.Manhattan: heuristicaInstancia = new Manhattan(); break;
            case HeuristicType.Euclidean: heuristicaInstancia = new Euclidean(); break;
            case HeuristicType.Chebyshev: heuristicaInstancia = new Chebyshev(); break;
        }
    }



    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos, NPCStats statsUnidad)
    {
        gridManager.ResetGridNodes();

        Node startNode = gridManager.NodeFromWorldPoint(startPos);
        Node targetNode = gridManager.NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null || !startNode.isWalkable || !targetNode.isWalkable)
            return null;

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                    currentNode = openSet[i];
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode);

            foreach (Node neighbor in gridManager.GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor)) continue;
                int penalizacionTerreno = (statsUnidad != null) ? statsUnidad.ObtenerCosteTerreno(neighbor.bioma) : 1;

                int costToNeighbor = GetStepCost(currentNode, neighbor);

                int newMovementCostToNeighbor = currentNode.gCost + costToNeighbor + penalizacionTerreno;
                
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetHeuristicDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }
        return null; 
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private int GetStepCost(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return (dx != 0 && dy != 0) ? 14 : 10;
    }

    private int GetHeuristicDistance(Node nodeA, Node nodeB)
    {
        // Seguridad por si no se ha inicializado
        if (heuristicaInstancia == null) InicializarHeuristica();

        Vector2Int posA = new Vector2Int(nodeA.gridX, nodeA.gridY);
        Vector2Int posB = new Vector2Int(nodeB.gridX, nodeB.gridY);

        return Mathf.RoundToInt(heuristicaInstancia.Evaluate(posA, posB) * 10f);
    }
}