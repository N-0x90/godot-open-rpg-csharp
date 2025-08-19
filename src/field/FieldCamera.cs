using Godot;
using System;
using OpenRPG.Field.Gameboard;

namespace OpenRPG.Field;

public partial class FieldCamera : Camera2D
{
    private GameboardProperties _gameboardProperties;

    [Export]
    public GameboardProperties GameboardProperties
    {
        get { return _gameboardProperties; }
        set { _gameboardProperties = value; }
    }

    private Gamepiece _gamepiece;

    [Export]
    public Gamepiece Gamepiece
    {
        get { return _gamepiece; }
        set { _gamepiece = value; }
    }
}
