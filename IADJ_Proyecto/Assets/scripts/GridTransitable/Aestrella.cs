using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    private GridManager gridManager;

    void Awake()
    {
        // Buscamos el GridManager que debe estar en este mismo objeto
        gridManager = GetComponent<GridManager>();
    }

    /// <summary>
    /// Encuentra el camino más corto entre dos posiciones del mundo.
    /// Pasamos el NPCStats para calcular el coste de terreno personalizado.
    /// Devuelve una lista de Nodos, o null si no hay camino posible.
    /// </summary>
    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos, NPCStats statsUnidad)
    {
        // IMPORTANTE: Reseteamos los nodos antes de empezar cualquier búsqueda
        gridManager.ResetGridNodes();

        Node startNode = gridManager.NodeFromWorldPoint(startPos);
        Node targetNode = gridManager.NodeFromWorldPoint(targetPos);

        // Seguridad: Si los nodos son nulos o no transitables, no buscamos camino
        if (startNode == null || targetNode == null || !startNode.isWalkable || !targetNode.isWalkable)
        {
            return null;
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Buscamos el nodo con el coste F más bajo en el openSet
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Si hemos llegado al destino, reconstruimos el camino
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            // Revisamos los vecinos
            foreach (Node neighbor in gridManager.GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                // Calculamos el coste para llegar a este vecino.
                // AQUÍ CALCULAMOS EL COSTE DEL TERRENO SEGÚN QUIÉN ESTÉ CAMINANDO
                int penalizacionTerreno = (statsUnidad != null) ? statsUnidad.ObtenerCosteTerreno(neighbor.biomaID) : 1;

                // F = G + Heurística(H) + PenalizaciónTerreno + Influencia(Peligro)
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor) + penalizacionTerreno + neighbor.influenceValue;
                
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        
        // Si sale del bucle es que no ha encontrado camino (ej. está rodeado de muros)
        return null; 
    }

    // Reconstruye el camino yendo hacia atrás desde el nodo final usando los "padres"
    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse(); // Le damos la vuelta para que vaya del inicio al fin
        return path;
    }

    // Calcula la distancia heurística entre dos nodos (usando 10 para rectas y 14 para diagonales)
    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
}
