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
        set
        {
            _gameboardProperties = value;
            OnViewportResized();
        }
    }

    private Gamepiece _gamepiece;

    [Export]
    public Gamepiece Gamepiece
    {
        get { return _gamepiece; }
        set
        {
            if (_gamepiece is not null)
                _gamepiece.AnimationTransform.RemotePath = "";
            
            _gamepiece = value; 
            
            if  (_gamepiece is not null)
                _gamepiece.AnimationTransform.RemotePath = _gamepiece.AnimationTransform.GetPathTo(this);
        }
    }

    public override void _Ready()
    {
        GetViewport().SizeChanged += OnViewportResized;
        OnViewportResized();
    }

    public void ResetPosition()
    {
        if (_gamepiece is not null)
            Position = _gamepiece.Position * Scale;
        
        ResetSmoothing();
    }

    private void OnViewportResized()
    {
        if (_gameboardProperties is null)
            return;

        // Calculate tentative camera boundaries based on the gameboard.
        var boundaryLeft = _gameboardProperties.Extents.Position.X * _gameboardProperties.CellSize.X;
        var boundaryTop = _gameboardProperties.Extents.Position.Y * _gameboardProperties.CellSize.Y;
        var boundaryRight = _gameboardProperties.Extents.End.X * _gameboardProperties.CellSize.X;
        var boundaryBottom = _gameboardProperties.Extents.End.Y * _gameboardProperties.CellSize.Y;

        var viewportSize = GetViewportRect().Size / GlobalScale;
        var boundaryWidth = boundaryRight - boundaryLeft;
        var boundaryHeight = boundaryBottom - boundaryTop;
        
        // If the boundary size is less than the viewport size, the camera limits will be smaller than
        // the camera dimensions (which does all kinds of crazy things in-game).
        // Therefore, if this is the case we'll want to centre the camera on the gameboard and set the
        // limits to be that of the viewport, locking the camera to one or both axes.
        // Start by checking the x-axis.
        // Note that the camera limits must be in global coordinates to function correctly, so account
        // using the global scale.
        if (boundaryWidth < viewportSize.X)
        {
            // Set the camera position to the centre of the gameboard.
            Position = new Vector2((_gameboardProperties.Extents.Position.X + _gameboardProperties.Extents.Size.X / 2f) * _gameboardProperties.CellSize.X, Position.Y);
            
            LimitLeft = (int)((Position.X - viewportSize.X / 2) * GlobalScale.X);
            LimitRight = (int)((Position.X + viewportSize.X / 2) * GlobalScale.X);
        }
        else
        {
            // If, however, the viewport is smaller than the gameplay area, the camera can be free to move as needed.
            LimitLeft = (int)(boundaryLeft * GlobalScale.X);
            LimitRight = (int)(boundaryRight * GlobalScale.X);
        }

        // 	Perform the same checks as above for the y-axis.
        if (boundaryHeight < viewportSize.Y)
        {
            Position = new Vector2(Position.X, _gameboardProperties.Extents.Position.Y + _gameboardProperties.Extents.Size.Y / 2f) * _gameboardProperties.CellSize.Y;
            
            LimitTop = (int)((Position.Y - viewportSize.Y / 2) * GlobalScale.Y);
            LimitBottom = (int)((Position.Y + viewportSize.Y / 2) * GlobalScale.Y);
        }
        else
        {
            LimitTop = (int)(boundaryTop * GlobalScale.Y);
            LimitBottom = (int)(boundaryBottom * GlobalScale.Y);
        }
    }
}
