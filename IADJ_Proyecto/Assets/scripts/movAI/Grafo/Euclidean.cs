using UnityEngine;

public class Euclidean : HeuristicaType
{
    public override bool AllowsDiagonal => true;

    public override float Evaluate(Vector2Int start, Vector2Int goal)
    {
        int dx = Mathf.Abs(start.x - goal.x);
        int dy = Mathf.Abs(start.y - goal.y);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
