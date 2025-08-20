using Godot;
using System;

namespace OpenRPG.Field.Gameboards;

/// <summary>
/// A wrapper for <see cref="AStar2D"/> that allows working with <see cref="Vector2I"/> coordinates.
/// Additionally, provides utility methods for easily dealing with cell availability and passability.
/// </summary>
public partial class Pathfinder : AStar2D
{
    /// <summary>
    /// When finding a path, we may want to ignore certain cells that are occupied by Gamepeices.
    /// These flags specify which disabled cells will still allow the path through.
    /// </summary>
    public enum PathfinderFlags
    {
        None                 = 0,
        /// <summary>
        /// Ignore the occupant of the source cell when searching for a path via [method path_to_cell].
        /// This is especially useful when wanting to find a path for a gamepiece from their current cell.
        /// </summary>
        AllowSourceOccupant  = 1 << 0,
        /// <summary>
        /// Ignore the occupant of the target cell when searching for a path via [method path_to_cell].
        /// </summary>
        AllowTargetOccupant  = 1 << 1,
        /// <summary>
        /// Ignore all gamepieces on the pathfinder cells when searching for a path via [method path_to_cell].
        /// </summary>
        AllowAllOccupants    = 1 << 2
    }
    
    public Pathfinder()
    {
        
    }
}
