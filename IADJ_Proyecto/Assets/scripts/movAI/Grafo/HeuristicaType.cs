using UnityEngine;

public abstract class HeuristicaType
{
    public abstract bool AllowsDiagonal { get; }
    public abstract float Evaluate(Vector2Int start, Vector2Int goal);
}
