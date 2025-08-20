using Godot;
using Godot.Collections;

namespace OpenRPG.Field.Gameboards;

/// <summary>
/// A wrapper for <see cref="AStar2D"/> that allows working with <see cref="Vector2I"/> coordinates.
/// Additionally, provides utility methods for easily dealing with cell availability and passability.
/// </summary>
public partial class Pathfinder : AStar2D
{
    // When finding a path, we may want to ignore certain cells that are occupied by Gamepieces.
    // These flags specify which disabled cells will still allow the path through.
    
    /// <summary>
    /// Ignore the occupant of the source cell when searching for a path via [method path_to_cell].
    /// This is especially useful when wanting to find a path for a gamepiece from their current cell.
    /// </summary>
    public const int FLAG_ALLOW_SOURCE_OCCUPANT = 1 << 0;
    
    /// <summary>
    /// Ignore the occupant of the target cell when searching for a path via [method path_to_cell].
    /// </summary>
    public const int FLAG_ALLOW_TARGET_OCCUPANT = 1 << 1;
    
    /// <summary>
    /// Ignore all gamepieces on the pathfinder cells when searching for a path via [method path_to_cell].
    /// </summary>
    public const int FLAG_ALLOW_ALL_OCCUPANTS = 1 << 2;
    
    public Pathfinder()
    {
        // todo: complete this
    }

    /// <summary>
    /// Returns true if the coordinate is found in the Pathfinder.
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public bool HasCell(Vector2I coord)
    {
        return HasPoint(Singletons.Gameboard.CellToIndex(coord));
    }

    /// <summary>
    /// Returns true if the coordinate is found in the Pathfinder and the cell is unoccupied.
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public bool CanMoveTo(Vector2I coord)
    {
        var uid = Singletons.Gameboard.CellToIndex(coord);
        return HasPoint(uid) && !IsPointDisabled(uid);
    }

    /// <summary>
    /// Find a path between two cells. Returns an empty array if no path is available.
    /// If allow_blocked_source or allow_blocked_target are false, the pathinder wlil fail if a gamepiece
    /// occupies the source or target cells, respectively.
    /// </summary>
    /// <param name="sourceCoord"></param>
    /// <param name="targetCoord"></param>
    /// <param name="occupancyFlags"></param>
    /// <returns></returns>
    public Array<Vector2I> GetPathToCell(Vector2I sourceCoord, Vector2I targetCoord, int occupancyFlags = 1)
    {
        // Store the return value in a variable.
        var movePath = new Array<Vector2I>();
        
        // Find the source/target IDs and keep track of whether or not the cells are occupied.
        var sourceId = Singletons.Gameboard.CellToIndex(sourceCoord);
        var targetId = Singletons.Gameboard.CellToIndex(targetCoord);
        
        // The pathfinder has several flags to ignore cell occupancy. We'll need to track which occupants
        // are temporarily ignored and then re-disable their pathfinder points once a path is found.
        // Key is point id, value is whether or not the point is disabled.
        var ignoredPoints = new Dictionary<long, bool>();
        if ((occupancyFlags & FLAG_ALLOW_ALL_OCCUPANTS) != 0)
        {
            foreach (var id in GetPointIds())
            {
                if (IsPointDisabled(id))
                {
                    ignoredPoints[id] = true;
                    SetPointDisabled(id, false);
                }
            }
        }

        if (HasPoint(sourceId) && HasPoint(targetId))
        {
            // Check to see if we want to un-disable the source/target cells.
            if ((occupancyFlags & FLAG_ALLOW_SOURCE_OCCUPANT) != 0)
            {
                ignoredPoints[sourceId] = IsPointDisabled(sourceId);
                SetPointDisabled(sourceId, false);
            }
            if ((occupancyFlags & FLAG_ALLOW_TARGET_OCCUPANT) != 0)
            {
                ignoredPoints[targetId] = IsPointDisabled(targetId);
                SetPointDisabled(targetId, false);
            }

            foreach (Vector2I pathCoord in GetPointPath(sourceId, targetId))
            {
                if (pathCoord != sourceCoord) // Don't include the source as the first path elemen
                    movePath.Add(pathCoord);
            }

            // Change any enabled cells back to their previous state.
            foreach (var kvp in ignoredPoints)
                SetPointDisabled(kvp.Key, kvp.Value);
        }

        return movePath;
    }

    /// <summary>
    /// Find a path to a cell adjacent to the target coordinate.
    /// Returns an empty path if there are no pathable adjacent cells.
    /// </summary>
    /// <param name="sourceCoord"></param>
    /// <param name="targetCoord"></param>
    /// <param name="occupancyFlags"></param>
    /// <returns></returns>
    public Array<Vector2I> GetPathCellsToAdjacentCell(Vector2I sourceCoord, Vector2I targetCoord, int occupancyFlags = 1)
    {
        var shortestPath = new Array<Vector2I>();
        var shortestPathLength = int.MaxValue;

        foreach (var cell in Singletons.Gameboard.GetAdjacentCells(targetCoord))
        {
            var cellPath = GetPathToCell(sourceCoord, cell, occupancyFlags);
            if (cellPath.Count > 0 && cellPath.Count < shortestPathLength)
            {
                shortestPathLength = cellPath.Count;
                shortestPath = cellPath;
            }
        }
        
        return shortestPath;
    }
}
