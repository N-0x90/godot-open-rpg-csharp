using Godot;
using System;
using Godot.Collections;

namespace OpenRPG.Common;

public partial class Directions : RefCounted
{
    /// <summary>
    /// The cardinal points, in clockwise order starting from North.
    /// </summary>
    public enum Points
    {
        North,
        East,
        South,
        West
    }
    
    public static readonly Dictionary<Points, Vector2I> Mappings =
        new()
        {
            { Points.North, Vector2I.Up },
            { Points.East,  Vector2I.Right },
            { Points.South, Vector2I.Down },
            { Points.West,  Vector2I.Left }
        };

    public static Points AngleToDirection(float angle)
    {
        if (angle <= Mathf.Pi / 4 && angle > -3 * Mathf.Pi / 4)
            return Points.North;
        
        if (angle <= Mathf.Pi / 4 && angle > -Mathf.Pi / 4)
            return Points.East;
        
        if (angle <= 3 * Mathf.Pi / 4 && angle > Mathf.Pi / 4)
            return Points.South;

        return Points.West;
    }

    public static Points VectorToDirection(Vector2 vector)
    {
        return AngleToDirection(vector.Angle());
    }
}
