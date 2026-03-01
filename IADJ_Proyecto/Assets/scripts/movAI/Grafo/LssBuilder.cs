using System;
using System.Collections.Generic;
using UnityEngine;

public class LssBuilder
{
    public List<Vector2Int> Build(Vector2Int start, int maxNodes, Func<Vector2Int, List<Vector2Int>> getTraversableNeighbours)
    {
        List<Vector2Int> lss = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        lss.Add(start);

        while (queue.Count > 0 && lss.Count < maxNodes)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int neighbour in getTraversableNeighbours(current))
            {
                // Si no está ya en el LSS y no hemos superado el límite
                if (!lss.Contains(neighbour) && lss.Count < maxNodes)
                {
                    lss.Add(neighbour);
                    queue.Enqueue(neighbour);
                }
            }
        }

        return lss;
    }
}
