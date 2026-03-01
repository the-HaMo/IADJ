using UnityEngine;

public class Manhattan : HeuristicaType
{
    public override bool AllowsDiagonal => false;

    public override float Evaluate(Vector2Int start, Vector2Int goal)
    {
        int dx = Mathf.Abs(start.x - goal.x);
        int dy = Mathf.Abs(start.y - goal.y);
        return dx + dy;
    }
}
