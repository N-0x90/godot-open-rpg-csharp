using Godot;
using System;
using OpenRPG.Field.Gamepieces;

namespace OpenRPG.Common;

/// <summary>
/// An autoload that provides easy access to the player's state, including both Combat and Field
/// details.
/// Reference to the player's party, inventory, and currently active character are found here.
/// Additionally, game-wide player based signals are emitted from here.
/// </summary>
public partial class Player : Node
{
    /// <summary>
    /// Emitted whenever the player's gamepiece changes.
    /// </summary>
    [Signal]
    public delegate void GamepieceChangedEventHandler();

    /// <summary>
    /// Emitted when the player sets a movement path for their focused gamepiece.
    /// The destination is the last cell in the path.
    /// </summary>
    [Signal]
    public delegate void PlayerPathSetEventHandler(Gamepiece gamepiece, Vector2I destinationCell);

    private Gamepiece _gamepiece;

    [Export]
    public Gamepiece Gamepiece
    {
        get => _gamepiece;
        set
        {
            if (_gamepiece != value)
            {
                _gamepiece = value;   
                EmitSignalGamepieceChanged();
            }
        }
    }
}
