using UnityEngine;

public class Node
{
    public bool isWalkable; // Si se puede transitar o hay un obstáculo
    public Vector3 worldPosition; // Posición real en el mundo (Unity 3D)
    public int gridX; // Posición X en la matriz del grid
    public int gridY; // Posición Y en la matriz del grid
    
    // Variables de Pathfinding y A* que necesitaremos pronto
    public int gCost; // Coste desde el nodo inicial
    public int hCost; // Coste heurístico hasta el nodo objetivo
    public Node parent; // Nodo 'padre' para reconstruir el camino
    
    // Variables para el PATHFINDING TÁCTICO
    public int terrainCost; // El coste de caminar por este tipo de terreno (ej. llano = 1, bosque = 2)
    public int influenceValue; // El valor de influencia/peligro de este nodo

    // Coste total (F = G + H + Terreno + InfluenciaTáctica)
    public int fCost
    {
        get { return gCost + hCost + terrainCost + influenceValue; }
    }

    public Node(bool _isWalkable, Vector3 _worldPos, int _gridX, int _gridY)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
        
        terrainCost = 1; // Por defecto
        influenceValue = 0; // Por defecto sin peligro
    }
}
