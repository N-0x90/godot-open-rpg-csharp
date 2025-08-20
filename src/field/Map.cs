using Godot;
using System;
using OpenRPG.Field.Gameboards;
using OpenRPG.Field.Gameboards.Debug;

namespace OpenRPG.Field;

/// <summary>
/// The map defines the properties of the playable grid, which will be applied on _ready to the
/// [Gameboard]. These properties usually correspond to one or multiple tilesets.
/// </summary>
[Tool]
public partial class Map : Node2D
{
    private GameboardProperties _gameboardProperties;

    [Export]
    public GameboardProperties GameboardProperties
    {
        get { return _gameboardProperties; }
        set
        {
            _gameboardProperties = value;
            
            if (!IsInsideTree())
            {
                CallDeferred(nameof(ApplyGameboardProperties));
                return;
            }

            ApplyGameboardProperties();
        }
    }
    
    private void ApplyGameboardProperties()
    {
        if (_debugGameboardBoundaries != null)
            _debugGameboardBoundaries.GameboardProperties = _gameboardProperties;
    }

    private DebugGameboardBoundaries _debugGameboardBoundaries;
    
    public override void _Ready()
    {
        _debugGameboardBoundaries = GetNode<DebugGameboardBoundaries>("Overlay/DebugBoundaries");
        
        if (!Engine.IsEditorHint())
        {
            Singletons.Camera.GameboardProperties = _gameboardProperties;
            Singletons.Gameboard.Properties = _gameboardProperties;
        }
    }
}
