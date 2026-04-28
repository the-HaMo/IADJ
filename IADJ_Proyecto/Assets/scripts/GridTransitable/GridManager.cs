using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class TerrainType
{
    public LayerMask terrainMask;
    [FormerlySerializedAs("biomaID")]
    public Bioma bioma;
}

[System.Serializable]
public class BiomaArea
{
    [FormerlySerializedAs("biomaID")]
    public Bioma bioma;
    public Vector2 centro;
    public Vector2 tamano;
}

public class GridManager : MonoBehaviour
{
    [Header("Configuracion del Grid")]
    public int nFilas = 105;
    public int nColumnas = 105;
    public float tamCelda = 3f;
    public LayerMask unwalkableMask;

    [Tooltip("La altura vertical de deteccion de muros. Ayuda a detectar muros aunque el grid no este a su altura exacta.")]
    public float obstacleDetectionHeight = 4f;

    [Header("Configuracion de Terrenos (Colliders)")]
    [Tooltip("Usa esto para caminos u obstaculos con formas complejas.")]
    public TerrainType[] biomasColliders;

    [Header("Configuracion de Terrenos (Coordenadas)")]
    [Tooltip("Usa esto para biomas rectangulares sin necesidad de usar colliders (mas eficiente).")]
    public BiomaArea[] biomasAreas;

    [Header("Depuracion")]
    [FormerlySerializedAs("mostrarGrid")]
    public bool debugGrid = false;

    Node[,] grid;
    Vector3 origenGrid;
    int gridSizeX;
    int gridSizeY;

    void Awake()
    {
        NormalizeSettings();
        UpdateOrigin();
        CreateGrid();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) debugGrid = !debugGrid;
    }

    void OnValidate()
    {
        NormalizeSettings();
        UpdateOrigin();
    }

    void NormalizeSettings()
    {
        nFilas = Mathf.Max(1, nFilas);
        nColumnas = Mathf.Max(1, nColumnas);
        tamCelda = Mathf.Max(0.01f, tamCelda);
        obstacleDetectionHeight = Mathf.Max(0.01f, obstacleDetectionHeight);
    }

    void UpdateOrigin()
    {
        gridSizeX = nColumnas;
        gridSizeY = nFilas;
        origenGrid = transform.position
            - Vector3.right * (gridSizeX * tamCelda) / 2f
            - Vector3.forward * (gridSizeY * tamCelda) / 2f;
    }

    public Vector3 GetWorldPosition(int fila, int columna)
    {
        return origenGrid + new Vector3(columna * tamCelda, 0f, fila * tamCelda);
    }

    public void GetGridPosition(Vector3 worldPosition, out int fila, out int columna)
    {
        columna = Mathf.FloorToInt((worldPosition.x - origenGrid.x) / tamCelda);
        fila = Mathf.FloorToInt((worldPosition.z - origenGrid.z) / tamCelda);

        columna = Mathf.Clamp(columna, 0, gridSizeX - 1);
        fila = Mathf.Clamp(fila, 0, gridSizeY - 1);
    }

    Vector3 GetCellCenter(int fila, int columna)
    {
        return GetWorldPosition(fila, columna) + new Vector3(tamCelda / 2f, 0f, tamCelda / 2f);
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];

        Vector3 boxExtents = new Vector3(tamCelda / 2f, obstacleDetectionHeight / 2f, tamCelda / 2f);

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = GetCellCenter(y, x);
                bool walkable = !Physics.CheckBox(worldPoint, boxExtents, Quaternion.identity, unwalkableMask);

                Bioma detectedBioma = Bioma.Pradera;

                if (walkable && biomasColliders != null && biomasColliders.Length > 0)
                {
                    foreach (TerrainType bioma in biomasColliders)
                    {
                        if (Physics.CheckBox(worldPoint, boxExtents, Quaternion.identity, bioma.terrainMask))
                        {
                            detectedBioma = bioma.bioma;
                            break;
                        }
                    }
                }

                if (walkable && detectedBioma == Bioma.Pradera && biomasAreas != null)
                {
                    foreach (var area in biomasAreas)
                    {
                        if (worldPoint.x >= area.centro.x - area.tamano.x / 2f &&
                            worldPoint.x <= area.centro.x + area.tamano.x / 2f &&
                            worldPoint.z >= area.centro.y - area.tamano.y / 2f &&
                            worldPoint.z <= area.centro.y + area.tamano.y / 2f)
                        {
                            detectedBioma = area.bioma;
                            break;
                        }
                    }
                }

                grid[x, y] = new Node(walkable, worldPoint, x, y);
                grid[x, y].bioma = detectedBioma;
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        if (grid == null) return null;
        GetGridPosition(worldPosition, out int fila, out int columna);
        return grid[columna, fila];
    }

    public Node GetNode(int columna, int fila)
    {
        if (grid == null) return null;
        if (columna < 0 || columna >= gridSizeX || fila < 0 || fila >= gridSizeY) return null;
        return grid[columna, fila];
    }

    public int GridSizeX => gridSizeX;
    public int GridSizeY => gridSizeY;

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

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

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

    void OnDrawGizmos()
    {
        if (!debugGrid) return;

        NormalizeSettings();
        UpdateOrigin();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSizeX * tamCelda, obstacleDetectionHeight, gridSizeY * tamCelda));

        Gizmos.color = Color.white;
        for (int fila = 0; fila <= gridSizeY; fila++)
        {
            Vector3 inicio = GetWorldPosition(fila, 0);
            Vector3 fin = GetWorldPosition(fila, gridSizeX);
            Gizmos.DrawLine(inicio, fin);
        }

        for (int columna = 0; columna <= gridSizeX; columna++)
        {
            Vector3 inicio = GetWorldPosition(0, columna);
            Vector3 fin = GetWorldPosition(gridSizeY, columna);
            Gizmos.DrawLine(inicio, fin);
        }

        if (biomasAreas != null)
        {
            foreach (var area in biomasAreas)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
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
                    Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                }
                else
                {
                    switch (n.bioma)
                    {
                        case Bioma.Camino:
                            Gizmos.color = new Color(0.6f, 0.3f, 0.1f, 0.5f);
                            break;
                        case Bioma.Bosque:
                            Gizmos.color = new Color(0.1f, 0.4f, 0.1f, 0.5f);
                            break;
                        case Bioma.Urbano:
                            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                            break;
                        default:
                            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f);
                            break;
                    }
                }

                Gizmos.DrawCube(n.worldPosition, Vector3.one * (tamCelda - 0.1f));
            }
        }
    }
}

