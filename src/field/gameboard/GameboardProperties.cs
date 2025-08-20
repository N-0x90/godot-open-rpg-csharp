using Godot;
using System;

namespace OpenRPG.Field.Gameboards;

/// <summary>
/// Defines the properties of the playable game map.
/// </summary>
[Tool]
public partial class GameboardProperties : Resource
{
    /// <summary>
    /// Emitted whenever <see cref="GameboardProperties.CellSize"/> changes.
    /// </summary>
    [Signal]
    public delegate void CellSizeChangedEventHandler();

    /// <summary>
    /// Emitted whenever <see cref="GameboardProperties.Extents"/> changes.
    /// </summary>
    [Signal]
    public delegate void ExtentsChangedEventHandler();
    
    /// <summary>
    /// ## An invalid index is not found on the gameboard. Note that this requires positive [member extents]
    /// </summary>
    public const int InvalidIndex = -1;

    private Rect2I _extents;

    /// <summary>
    /// The extents of the playable area. This property is intended for editor use and should not change
    /// during gameplay, as that would change how [Pathfinder] indices are calculated.
    /// </summary>
    [Export]
    public Rect2I Extents
    {
        get => _extents;
        set
        {
            if (_extents == value)
                return;
            
            // Ensure that the boundary size is greater than 0.
            _extents = new Rect2I(value.Position, Mathf.Max(value.Size.X, 1), Mathf.Max(value.Size.Y, 1));
            
            EmitSignalExtentsChanged();
        }
    }

    private Vector2I _cellSize;

    /// <summary>
    /// ## The size of each grid cell. Usually analogous to a [member TileSet.tile_size] of a [GameboardLayer].
    /// </summary>
    [Export]
    public Vector2I CellSize
    {
        get => _cellSize;
        set
        {
            if (_cellSize == value)
                return;
            
            _cellSize = value; 
            HalfCellSize = _cellSize / 2;
            
            EmitSignalCellSizeChanged();
        }
    }

    public Vector2I HalfCellSize = new (8, 8);
}
