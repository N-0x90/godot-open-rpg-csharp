using Godot;
using Godot.Collections;
using OpenRPG.Common;
using OpenRPG.Field.Gameboards;

namespace OpenRPG.Field.Gameboards;

/// <summary>
/// Defines the playable area of the game and where everything on it lies.
/// The gameboard is defined, essentially, as a grid of [Vector2i] cells. Anything may be
/// placed on one of these cells, so the gameboard determines where each cell is located. In this
/// case, we are using a simple orthographic (square) projection.
/// [br][br]The grid is contained within the playable [member boundaries] and its constituent cells.
/// </summary>
public partial class Gameboard : Node
{
    /// <summary>
    /// Emitted whenever [member properties] is set. This is used in case a [Gamepiece] is added to the
    /// board before the board properties are ready
    /// </summary>
    [Signal]
    public delegate void PropertiesSetEventHandler();

    /// <summary>
    /// Emitted whenever the [member pathfinder] state changes.
    /// This signal is emitted automatically in response to changed [GameboardLayer]s.
    /// Note: This signal is only emitted when the actual movement state of the Gameboard
    /// changes. [GameboardLayer]s may change their cells without actually changing the pathfinder's
    /// state (i.e. a visual update only), in which case this signal is not emitted.
    /// </summary>
    [Signal]
    public delegate void PathfinderChangedEventHandler(Array<Vector2I> addedCells, Array<Vector2I> removedCells);
    
    /// <summary>
    /// An invalid cell is not part of the gameboard. Note that this requires positive
    /// [member boundaries].
    /// </summary>
    public readonly Vector2I InvalidCell = new (-1, -1);
    
    private readonly int InvalidIndex = -1;
    
    private GameboardProperties _properties;

    public GameboardProperties Properties
    {
        get { return _properties; }
        set
        {
            if (_properties == value)
                return;
            
            _properties = value;
            EmitSignalPropertiesSet();
        }
    }
    
    /// <summary>
    /// A reference to the Pathfinder for the current playable area.
    /// </summary>
    public Pathfinder Pathfinder = new ();

    /// <summary>
    /// Convert cell coordinates to pixel coordinates.
    /// </summary>
    /// <param name="cellCoordinates"></param>
    /// <returns></returns>
    public Vector2 CellToPixel(Vector2I cellCoordinates)
    {
        // warning: not a direct constructor from Vector2I
        return new Vector2(cellCoordinates.X * _properties.CellSize.X, cellCoordinates.Y * _properties.CellSize.Y) + _properties.HalfCellSize;
    }

    /// <summary>
    /// Convert pixel coordinates to cell coordinates.
    /// </summary>
    /// <param name="pixelCoordinates"></param>
    /// <returns></returns>
    public Vector2I PixelToCell(Vector2 pixelCoordinates)
    {
        return new Vector2I(
            Mathf.FloorToInt(pixelCoordinates.X / _properties.CellSize.X),
            Mathf.FloorToInt(pixelCoordinates.Y / _properties.CellSize.Y)
            );
    }

    public Vector2I GetCellUnderNode(Node2D node)
    {
        return PixelToCell(node.GlobalPosition / node.GlobalScale);
    }

    /// <summary>
    /// Convert cell coordinates to an index unique to those coordinates.
    /// [br][br][b]Note:[/b] cell coordinates outside the [member extents] will return
    /// [constant INVALID_INDEX].
    /// </summary>
    /// <param name="cellCoordinates"></param>
    /// <returns></returns>
    public int CellToIndex(Vector2I cellCoordinates)
    {
        if (!_properties.Extents.HasPoint(cellCoordinates))
            return InvalidIndex;
        
        // Negative coordinates can throw off index generation, so offset the boundary so that it's
        // top left corner is always considered Vector2i.ZERO and index 0.
        return (cellCoordinates.X - _properties.Extents.Position.X) 
               + (cellCoordinates.Y - _properties.Extents.Position.Y) 
               * _properties.Extents.Size.X;
    }

    /// <summary>
    /// Convert a unique index to cell coordinates.
    /// [br][br][b]Note:[/b] indices outside the gameboard [member GameboardProperties.extents] will
    /// return [constant INVALID_CELL].
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector2I IndexToCell(int index)
    {
        var cell = new Vector2I(
            index % _properties.Extents.Size.X + _properties.Extents.Position.X,
            index / _properties.Extents.Size.X + _properties.Extents.Position.Y);
        
        return _properties.Extents.HasPoint(cell) ? cell : InvalidCell;
    }

    /// <summary>
    /// Find a neighbouring cell, if it exists. Otherwise, returns [constant INVALID_CELL].
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Vector2I GetAdjacentCell(Vector2I cell, int direction)
    {
        Directions.Mappings.TryGetValue((Directions.Points)direction, out var value);
        var neighbour = cell + value;
        return _properties.Extents.HasPoint(neighbour) ? neighbour : InvalidCell;
    }

    /// <summary>
    /// Find all cells adjacent to a given cell. Only existing cells will be included.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public Array<Vector2I> GetAdjacentCells(Vector2I cell)
    {
        var neighbours = new Array<Vector2I>();
        for (int i = 0; i < 4; i++)
        {
            // var direction = (Directions.Points)i;
            var neighbour = GetAdjacentCell(cell, i);
            if (neighbour != InvalidCell && neighbour != cell)
                neighbours.Add(neighbour);
        }
        return neighbours;
    }

    /// <summary>
    /// The Gameboard's state (where [Gamepiece]'s may or may not move) is composed from a number of
    /// [GameboardLayer]s. These layers determine which cells are blocked or clear.
    /// The layers register themselves to the Gameboard in _ready.
    /// </summary>
    /// <param name="boardMap"></param>
    public void RegisterGameboardLayer(GameboardLayer boardMap)
    {
        // We want to know whenever the board_map changes the gameboard state. This occurs when the map
        // is added or removed from the scene tree, or when its list of moveable cells changes.
        // Compare the changed cells with those already in the pathfinder. Any changes will cause the
        // Pathfinder to be updated.
        
        boardMap.CellsChanged += (clearedCells, blockedCells) =>
        {
            if (boardMap.Name == "DoorGameboardLayer")
                GD.Print("Door layer ", clearedCells, " ", blockedCells);

            var addedCells = AddCellsToPathfinder(clearedCells);
            var removedCells = RemoveCellsFromPathfinder(blockedCells);
            
            ConnectNewPathfinderCells(addedCells);
            if (addedCells.Count > 0 || removedCells.Count > 0)
                EmitSignalPathfinderChanged((Array<Vector2I>)addedCells.Values, removedCells);
        };
    }

    /// <summary>
    /// Add cells to the pathfinder, checking that there are no blocking tiles on any GameboardLayers.
    /// Returns a dictionary representing the cells that are actually added to the pathfinder (may differ
    /// from cleared_cells). Key = cell id (int, see cell_to_index), value = coordinate (Vector2i)
    /// </summary>
    /// <param name="clearedCells"></param>
    /// <returns></returns>
    private Dictionary<int, Vector2I> AddCellsToPathfinder(Array<Vector2I> clearedCells)
    {
        var addedCells = new Dictionary<int, Vector2I>();

        // Verify whether or not cleared/blocked cells will change the state of the pathfinder.
        // If there is no change in state, we will not pass along the cell to other systems and
        // the pathfinder won't actually be changed.
        foreach (var cell in clearedCells)
        {
            // Note that cleared cells need to have all layers checked for a blocking tile.
            if (_properties.Extents.HasPoint(cell) && !Pathfinder.HasCell(cell) && IsCellClear(cell))
            {
                var uid = CellToIndex(cell);
                addedCells[uid] = cell;
                
                // Flag the cell as disabled if it is occupied.
                if (Singletons.GamepieceRegistry.GetGamepiece(cell) is not null)
                    Pathfinder.SetPointDisabled(uid);
            }
        }
        
        return addedCells;
    }
    
    /// <summary>
    /// Remove cells from the pathfinder so that Gamepieces can no longer move through them.
    /// Only one Gameboard layer needs to block a cell for it to be considered blocked.
    /// Returns an array of cell coordinates that have been blocked. Cells that were already not in the
    /// pathfinder will be excluded from this array.
    /// </summary>
    /// <param name="blockedCells"></param>
    /// <returns></returns>
    private Array<Vector2I> RemoveCellsFromPathfinder(Array<Vector2I> blockedCells)
    {
        var removedCells = new Array<Vector2I>();

        foreach (var cell in blockedCells)
        {
            // Only remove a cell that is already in the pathfinder. Also, we need to check that the cell
            // is not clear, since this method is also called when cells are removed from GameboardLayers
            // and other layers may still have this cell on their map.
            if (Pathfinder.HasCell(cell) && !IsCellClear(cell))
            {
                Pathfinder.RemovePoint(CellToIndex(cell));
                removedCells.Add(cell);
            }
        }
        
        return removedCells;
    }

    /// <summary>
    /// Go through a list of cells added to the pathfinder (returned from _add_cells_to_pathfinder) and
    /// connect them to each other and existing pathfinder cells.
    /// </summary>
    /// <param name="addedCells"></param>
    private void ConnectNewPathfinderCells(Dictionary<int, Vector2I> addedCells)
    {
        foreach (var cell in addedCells)
        {
            if (Pathfinder.HasPoint(cell.Key))
            {
                foreach (var neighbor in GetAdjacentCells(cell.Value))
                {
                    var neighborId = CellToIndex(neighbor);
                    if (Pathfinder.HasPoint(neighborId))
                    {
                        Pathfinder.ConnectPoints(cell.Key, neighborId);
                    }
                }
            }
        }
    }

    private bool IsCellClear(Vector2I coord)
    {
        var cellExists = false;

        foreach (var node in GetTree().GetNodesInGroup(GameboardLayer.Group))
        {
            if (node is GameboardLayer tilemap)
            {
                if (tilemap.GetUsedCells().Contains(coord))
                {
                    cellExists = true;
                    if (!tilemap.IsCellClear(coord))
                    {
                        return false;
                    }
                }
            }
        }
        
        return cellExists;
    }
}
