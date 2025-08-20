using Godot;
using System;
using System.Linq;
using Godot.Collections;

namespace OpenRPG.Field.Gamepieces;

/// <summary>
/// The GamepieceRegistry keeps track of each [Gamepiece] that is currently placed on the board.
/// Since player movement is locked to gameboard cells (rather than physics-based movement), this
/// allows UI elements, cutscenes, and other systems to quickly lookup gamepieces by name or
/// location. Additionally, it allows pathfinders to see which cells are occupied or unoccupied.
/// Note that only ONE gamepiece may occupy a cell and will block the movement of other
/// gamepieces.
/// </summary>
public partial class GamepieceRegistry : Node
{
    [Signal]
    public delegate void GamepieceMovedEventHandler(Gamepiece gamepiece, Vector2I newCell, Vector2I oldCell);

    [Signal]
    public delegate void GamepieceFreedEventHandler(Gamepiece gamepiece, Vector2I cell);

    /// <summary>
    /// Store all registered Gamepeices by the cell they occupy.
    /// </summary>
    private Dictionary<Vector2I, Gamepiece> _gamepieces;

    public bool Register(Gamepiece gamepiece, Vector2I cell)
    {
        // Don't register a gamepiece if it's cell is already occupied...
        if (_gamepieces.ContainsKey(cell))
        {
            GD.PrintErr($"Failed to register Gamepiece '{gamepiece.Name}' at cell '{cell}'. A gamepiece already exists at that cell!");
            return false;
        }

        // or if it has already been registered, for some reason.
        if (_gamepieces.Values.Contains(gamepiece))
        {
            GD.PrintErr($"Refused to register Gamepiece '{gamepiece.Name}' at cell '{cell}'. The gamepiece has already been registered!");
            return false;
        }
        
        // We want to know when the gamepiece leaves the scene tree, as it is no longer on the gameboard.
        // This probably means that the gamepiece has been freed.
        gamepiece.TreeExiting += () => OnGamePieceTreeExiting(gamepiece);
        
        _gamepieces.Add(cell, gamepiece);
        EmitSignalGamepieceMoved(gamepiece, cell, Singletons.Gameboard.InvalidCell);
        
        return true;
    }

    /// <summary>
    /// Update a gamepiece's position within the registry.
    /// Note that animation/position will need to be updated in response to the
    /// [signal Events.gamepiece_moved] signal
    /// </summary>
    /// <param name="gamepiece"></param>
    /// <param name="newCell"></param>
    /// <returns></returns>
    public bool MoveGamepiece(Gamepiece gamepiece, Vector2I newCell)
    {
        // Don't move a gamepiece to an occupied cell.
        if (_gamepieces.ContainsKey(newCell))
        {
            GD.Print($"Cell {newCell} is already occupied, cannot move gamepiece '{gamepiece.Name}'!");
            return false;
        }

        var oldCell = GetCell(gamepiece);
        if (oldCell == newCell)
        {
            return false;
        }
        
        _gamepieces.Remove(oldCell);
        _gamepieces.Add(newCell, gamepiece);

        EmitSignalGamepieceMoved(gamepiece, newCell, oldCell);
        
        return true;
    }

    /// <summary>
    /// Return the gamepiece, if any,that is found at a given cell.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public Gamepiece GetGamepiece(Vector2I cell)
    {
        return _gamepieces[cell];
    }

    /// <summary>
    /// Return the gamepiece, if any, that has a given name.
    /// </summary>
    /// <param name="gamepieceName"></param>
    /// <returns></returns>
    public Gamepiece GetGamepieceByName(string gamepieceName)
    {
        var gamepiece = _gamepieces
            .Values
            .FirstOrDefault(f => f.Name == gamepieceName);
        
        return gamepiece;
    }

    /// <summary>
    /// Return the cell occupied by a given gamepiece.
    /// </summary>
    /// <param name="gamepiece"></param>
    /// <returns></returns>
    public Vector2I GetCell(Gamepiece gamepiece)
    {
        // Don't look up null gamepieces (blocked cells).
        if (gamepiece is not null)
        {
            foreach (var kvp in _gamepieces)
            {
                if (kvp.Value == gamepiece)
                {
                    return kvp.Key;
                }
            }
        }
        
        return Singletons.Gameboard.InvalidCell;
    }

    public Array<Vector2I> GetOccupiedCells()
    {
        return (Array<Vector2I>)_gamepieces.Keys;
    }

    public Array<Gamepiece> GetGamepieces()
    {
        return (Array<Gamepiece>)_gamepieces.Values;
    }

    /// <summary>
    /// Remove all traces of the gamepiece from the registry.
    /// </summary>
    /// <param name="gamepiece"></param>
    private void OnGamePieceTreeExiting(Gamepiece gamepiece)
    {
        var cell = GetCell(gamepiece);
        if (_gamepieces.Remove(cell))
        {
            EmitSignalGamepieceFreed(gamepiece, cell);
        }
    }
}
