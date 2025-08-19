using Godot;
using System;

namespace OpenRPG.Field.Gameboard.Debug;

/// <summary>
/// Draws the boundaries set by a [Gameboard] object.
/// Used within the editor to illustrate which cells will be included in the pathfinder calculations.
/// </summary>
public partial class DebugGameboardBoundaries : Node2D
{
    private GameboardProperties _gameboardProperties;

    [Export]
    public GameboardProperties GameboardProperties
    {
        get { return _gameboardProperties; }
        set
        {
            _gameboardProperties = value; 
            
            // For some reason, Godot 4.4.1 won't connect to gameboard_properties signals here, so its
            // done on _ready instead. This means that the scene may need to be loaded before the
            // debug boundaries will automatically update.
            UpdateBoundaries();
        }
    }

    private Color _boundaryColor;

    [Export]
    public Color BoundaryColor
    {
        get { return _boundaryColor; }
        set
        {
            _boundaryColor = value; 
            QueueRedraw();
        }
    }
    
    private float _lineWidth = 2.0f;
    
    [Export(PropertyHint.Range, "0.5, 5.0, 0.1, or_greater")]
    public float LineWidth
    {
        get => _lineWidth;
        set
        {
            _lineWidth = value;
            QueueRedraw();
        }
    }

    private Rect2I _boundaries;

    public override void _Ready()
    {
        if (_gameboardProperties is not null)
        {
            _gameboardProperties.ExtentsChanged += UpdateBoundaries;
            _gameboardProperties.CellSizeChanged += UpdateBoundaries;
        }
        
        if (Engine.IsEditorHint())
            Hide();
    }

    public override void _Draw()
    {
        if (_gameboardProperties is null)
            return;
        
        DrawRect(_boundaries, _boundaryColor, false, _lineWidth);
    }

    private void UpdateBoundaries()
    {
        if (_gameboardProperties is null)
            return;
        
        _boundaries = new Rect2I(_gameboardProperties.Extents.Position * _gameboardProperties.CellSize, 
            _gameboardProperties.Extents.Size * _gameboardProperties.CellSize);
    }
}
