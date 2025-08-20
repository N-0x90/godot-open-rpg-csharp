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
}
