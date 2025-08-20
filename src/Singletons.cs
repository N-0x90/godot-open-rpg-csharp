using Godot;
using OpenRPG.Field.Gamepieces;
using OpenRPG.Gameboards;

namespace OpenRPG;

public static class Singletons
{
    private static Gameboard _gameboard;

    public static Gameboard Gameboard
    {
        get
        {
            if  (_gameboard is null)
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
}