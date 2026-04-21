using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Configuración del Grid")]
    public LayerMask unwalkableMask; // Capa(s) Unity que define los obstáculos intransitables (ej. "Muros", "Agua_Profunda")
    public Vector2 gridWorldSize; // Tamaño del Grid en coordenadas del mundo
    public float nodeRadius; // Radio de cada nodo/casilla
    
    [Header("Depuración")]
    public bool mostrarGrid = true; // Activa o desactiva las celdas rojas/blancas en la escena

    Node[,] grid;
    float nodeDiameter;
    int gridSizeX, gridSizeY;

    void Awake()
    {
        nodeDiameter = nodeRadius * 2f;
        
        // Calculamos la cantidad de nodos que caben en el tamaño del grid (redondeando con Mathf.RoundToInt)
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        
        // Calculamos la esquina inferior izquierda del grid
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Hallamos la posición en el mundo de este nodo actual
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                
                // Determinamos si es transitable (¿Choca nuestra esfera imaginaria con la máscara de obstáculos?)
                // Si Physics.CheckSphere choca contra la 'unwalkableMask', significa que NO es transitable
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        // Convertimos la posición del mundo en porcentajes relativos a nuestro Grid (0 a 1)
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        
        // Clampeamos para no salirnos de los límites si el punto está muy lejos
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }
    
    // Lista de vecinos de un nodo para el A*
    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Saltamos a nosotros mismos
                
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    // Dibujamos el Grid en el Editor (Modo Depuración visual) para ver si todo encaja
    void OnDrawGizmos()
    {
        if (!mostrarGrid) return; // <-- La cajita de tick que apaga y enciende la magia visual

        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null)
        {
            foreach (Node n in grid)
            {
                // Si es transitable será blanco translúcido, si es obstáculo será rojo.
                Gizmos.color = n.isWalkable ? new Color(1, 1, 1, 0.3f) : new Color(1, 0, 0, 0.5f);
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }
}
