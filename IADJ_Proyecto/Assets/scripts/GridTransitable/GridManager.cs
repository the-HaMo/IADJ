using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainType
{
    public LayerMask terrainMask;
    public int biomaID; // ID numérico (Ej: 1=Camino, 2=Bosque)
}

[System.Serializable]
public class BiomaArea
{
    public int biomaID;
    public Vector2 centro;
    public Vector2 tamano;
}

public class GridManager : MonoBehaviour
{
    [Header("Configuración del Grid")]
    public LayerMask unwalkableMask; // Capa(s) Unity que define los obstáculos intransitables (ej. "Muros", "Agua_Profunda")
    public Vector2 gridWorldSize; // Tamaño del Grid en coordenadas del mundo
    public float nodeRadius; // Radio de cada nodo/casilla
    
    [Tooltip("La altura vertical de detección de muros. Ayuda a detectar muros aunque el grid no esté a su altura exacta.")]
    public float obstacleDetectionHeight = 4f; 
    
    [Header("Configuración de Terrenos (Colliders)")]
    [Tooltip("Usa esto para caminos u obstáculos con formas complejas.")]
    public TerrainType[] biomasColliders;

    [Header("Configuración de Terrenos (Coordenadas)")]
    [Tooltip("Usa esto para biomas rectangulares sin necesidad de usar colliders (más eficiente).")]
    public BiomaArea[] biomasAreas;

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

        // Definimos las dimensiones de la "caja" de colisión para cada nodo. 
        // X y Z son el radio del nodo, Y es la altura configurable para pillar muros altos o bajos.
        Vector3 boxExtents = new Vector3(nodeRadius, obstacleDetectionHeight / 2f, nodeRadius);

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Hallamos la posición en el mundo de este nodo actual
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                
                // En lugar de una esfera, usamos una caja vertical. Así no hace falta que el grid esté clavado a la altura del muro.
                bool walkable = !Physics.CheckBox(worldPoint, boxExtents, Quaternion.identity, unwalkableMask);
                
                int detectedBiomaID = 0; // Coste base o Llanura (0)

                // 1. PRIORIDAD ALTA: Biomas por Colliders (Caminos y formas complejas)
                if (walkable && biomasColliders != null && biomasColliders.Length > 0)
                {
                    foreach (TerrainType bioma in biomasColliders)
                    {
                        if (Physics.CheckBox(worldPoint, boxExtents, Quaternion.identity, bioma.terrainMask))
                        {
                            detectedBiomaID = bioma.biomaID;
                            break; // Si encontramos un camino, ese manda
                        }
                    }
                }

                // 2. PRIORIDAD BAJA: Biomas por Coordenadas (Bosques grandes, Praderas, etc.)
                // Solo si no se ha detectado un camino/bioma complejo antes
                if (walkable && detectedBiomaID == 0 && biomasAreas != null)
                {
                    foreach (var area in biomasAreas)
                    {
                        if (worldPoint.x >= area.centro.x - area.tamano.x / 2 &&
                            worldPoint.x <= area.centro.x + area.tamano.x / 2 &&
                            worldPoint.z >= area.centro.y - area.tamano.y / 2 &&
                            worldPoint.z <= area.centro.y + area.tamano.y / 2)
                        {
                            detectedBiomaID = area.biomaID;
                            break;
                        }
                    }
                }
                
                grid[x, y] = new Node(walkable, worldPoint, x, y);
                grid[x, y].biomaID = detectedBiomaID; // Guardamos el ID del bioma en el nodo
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        // Convertimos la posición del mundo en porcentajes relativos a nuestro Grid (0 a 1), teniendo en cuenta la posición del GridManager
        float percentX = (worldPosition.x - transform.position.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z - transform.position.z + gridWorldSize.y / 2) / gridWorldSize.y;
        
        // Clampeamos para no salirnos de los límites si el punto está muy lejos
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    // Limpia los costes de los nodos para que el A* pueda empezar de cero en cada búsqueda
    public void ResetGridNodes()
    {
        if (grid == null) return;
        foreach (Node n in grid)
        {
            n.gCost = 0;
            n.hCost = 0;
            n.parent = null;
        }
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

        // Dibujamos una caja amarilla que representa el VOLUMEN TOTAL donde se buscan los muros
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, obstacleDetectionHeight, gridWorldSize.y));

        // Dibujamos las áreas de biomas por coordenadas
        if (biomasAreas != null)
        {
            foreach (var area in biomasAreas)
            {
                Gizmos.color = new Color(0, 1, 1, 0.2f); // Cian transparente
                Gizmos.DrawCube(new Vector3(area.centro.x, transform.position.y, area.centro.y), new Vector3(area.tamano.x, 0.1f, area.tamano.y));
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(new Vector3(area.centro.x, transform.position.y, area.centro.y), new Vector3(area.tamano.x, 0.1f, area.tamano.y));
            }
        }

        if (grid != null)
        {
            foreach (Node n in grid)
            {
                if (!n.isWalkable)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.5f); // Rojo para obstáculos (Muros)
                }
                else
                {
                    // Pintar de diferentes colores según el bioma detectado
                    switch (n.biomaID)
                    {
                        case 1: Gizmos.color = new Color(0.6f, 0.3f, 0.1f, 0.5f); break; // Marrón (Camino)
                        case 2: Gizmos.color = new Color(0.1f, 0.4f, 0.1f, 0.5f); break; // Verde Oscuro (Bosque)
                        case 3: Gizmos.color = new Color(1.0f, 0.5f, 0.0f, 0.5f); break; // Naranja (Urbano)
                        default: Gizmos.color = new Color(0.5f, 1.0f, 0.5f, 0.3f); break;// Verde Claro (Pradera / Por defecto)
                    }
                }
                
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
    }
}
