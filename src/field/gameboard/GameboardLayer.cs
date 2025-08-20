using Godot;
using System;
using Godot.Collections;

namespace OpenRPG.Field.Gameboards;

/// <summary>
/// ## A variation of [TileMapLayer] that is used to create the [Gameboard].
/// Comes setup with a few tools used by designers to setup maps, specifying which cells may be
/// moved to or from. Multiple GameboardLayers may exist simultaneously to allow developers to
/// affect the gameboard with more than one layer.
/// These collision layers may also dynamically change, with the changes being reflected
/// by the gameboard and pathfinder.
/// </summary>
public partial class GameboardLayer : TileMapLayer
{
    /// <summary>
    /// Emitted whenever the collision state of the tile map changes.
    /// The GameboardLayer's tile map may change without changing which cells are blocked or
    /// are open for movement. In this case, the signal is not emitted. However, it is emitted whenever
    /// the map is added or removed from the game, in addition to when there is a change in blocked/clear
    /// cells.
    /// </summary>
    [Signal]
    public delegate void CellsChangedEventHandler(Array<Vector2I> clearedCells, Array<Vector2I> blockedCells);

    /// <summary>
    /// The group name of all [GameboardLayer] that will be checked for blocked/walkable cells.
    /// </summary>
    public const string Group = "GameboardTileMapLayers";

    /// <summary>
    /// The name of the "custom data layer" that determines whether or not a cell is blocked/walkable.
    /// The returned value will be a boolean reflecting if a cell is blocked or not. Fetches the value
    /// via [method TileData.get_custom_data].
    /// If the data layer is not present in the [Tileset], then all cells in this [TileMapLayer] will,
    /// by default, block movement.
    /// </summary>
    public const string BlockedCellDataLayer = "IsCellBlocked";

    /// <summary>
    /// A false value will cause is_cell_clear to always return true. This is used to flag when the
    /// TileMapLayers is being cleaned up an should no longer affect the pathfinder.
    /// </summary>
    private bool _affectedCollision = true;

    public override void _Ready()
    {
        AddToGroup(Group);
        Singletons.Gameboard.RegisterGameboardLayer(this);

        TreeExiting += () =>
        {
            _affectedCollision = false;

            var blockedCells = new Array<Vector2I>();
            EmitSignalCellsChanged(GetUsedCells(), blockedCells);
        };
    }

    /// <summary>
    /// Returns true if the tile at coord exists and does not have a custom blocking data layer with a
    /// value set to true.
    /// Otherwise, returns fals
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public bool IsCellClear(Vector2I coord)
    {
        if (!_affectedCollision)
            return true;

        var tileData = GetCellTileData(coord);
        if (tileData is null)
            // If the above conditions have not been met, the cell is blocked.
            return false;
        
        var isCellBlocked = (bool)tileData.GetCustomData(BlockedCellDataLayer);
        return !isCellBlocked;
    }

    /// <summary>
    /// See [method TileMapLayer._update_cells]; called whenever the cells change. This allows designers
    /// to change maps on the fly and the collision state of the pathfinder should update. The coords
    /// parameter lets us know which cells have changed. Also, the method is called as the TileMapLayer
    /// is added to the scene.
    /// Note that if forced_cleanup is true, the TileMapLayer is in a state where its tiles should not
    /// affect collision. The conditions causing forced_cleanup are handled seperately through signals
    /// found in _ready().
    /// </summary>
    /// <param name="coords"></param>
    /// <param name="forcedCleanup"></param>
    public override void _UpdateCells(Array<Vector2I> coords, bool forcedCleanup)
    {
        // First of all, check to make sure the the tilemap has a tileset and the specific custom data
        // layer that we need to specify whether or not a tile blocks movement.
        if (TileSet is null || !TileSet.HasCustomDataLayerByName(BlockedCellDataLayer))
            return;
        
        // Go through the specified coords, checking to see if any moveable cells (those that are NOT
        // blocked) have been added or removed.
        var clearedCells = new Array<Vector2I>();
        var blockedCells = new Array<Vector2I>();

        if (!forcedCleanup)
        {
            foreach (var coord in coords)
            {
                if (IsCellClear(coord))
                    clearedCells.Add(coord);
                else
                    blockedCells.Add(coord);
            }
        }
        
        if (clearedCells.Count > 0 || blockedCells.Count > 0)
            EmitSignalCellsChanged(clearedCells, blockedCells);
    }
}
