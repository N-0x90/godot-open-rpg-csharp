using Godot;
using OpenRPG.Common;
using OpenRPG.Field;
using OpenRPG.Field.Gamepieces;
using OpenRPG.Field.Gameboards;

namespace OpenRPG;

public static class Singletons
{
    private static FieldCamera _camera;

    public static FieldCamera Camera
    {
        get
        {
            if (_camera == null)
                _camera = GD.Load<FieldCamera>("/root/Camera");

            return _camera;
        }
    }

    private static Gameboard _gameboard;

    public static Gameboard Gameboard
    {
        get
        {
            if (_gameboard is null)
                _gameboard = GD.Load<Gameboard>("/root/Gameboard");

            return _gameboard;
        }
    }

    private static GamepieceRegistry _gamepieceRegistry;

    public static GamepieceRegistry GamepieceRegistry
    {
        get
        {
            if (_gamepieceRegistry is null)
                _gamepieceRegistry = GD.Load<GamepieceRegistry>("/root/GamepieceRegistry");

            return _gamepieceRegistry;
        }
    }

    private static Player _player;

    public static Player Player
    {
        get
        {
            if (_player is null)
                _player = GD.Load<Player>("/root/Player");

            return _player;
        }
    }

    private static FieldEvents _fieldEvents;

    public static FieldEvents FieldEvents
    {
        get
        {
            if (_fieldEvents is null)
                _fieldEvents = GD.Load<FieldEvents>("/root/FieldEvents");

            return _fieldEvents;
        }
    }
}