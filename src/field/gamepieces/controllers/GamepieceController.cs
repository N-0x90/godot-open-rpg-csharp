using Godot;
using Godot.Collections;
using Array = System.Array;

namespace OpenRPG.Field.Gamepieces.Controllers;

[Tool]
[Icon("res://assets/editor/icons/IconGamepieceController.svg")]
public partial class GamepieceController : Node2D
{
    /// <summary>
    /// Emitted whenever the gamepiece begins moving towards a new cell in its [member move_path].
    /// </summary>
    [Signal]
    public delegate void WaypontChangedEventHandler(Vector2I waypoint);

    private bool _isActive;

    /// <summary>
    /// An active controller will receive inputs (player or otherwise). An inactive controller does
    /// nothing. This is useful, for example, when toggling of gamepiece movement during cutscenes.
    /// </summary>
    public bool IsActive
    {
        get { return _isActive; }
        set { SetIsActive(value); }
    }

    private Array<Vector2I> _movePath;

    /// <summary>
    /// Keep track of a move path. The controller will check that the path is clear each time the 
    /// gamepiece needs to continue on to the next cell.
    /// </summary>
    public Array<Vector2I> MovePath
    {
        get { return _movePath; }
        set { MoveAlongPath(value); }
    }

    private Vector2I _currentWaypoint;

    public Vector2I CurrentWaypoint
    {
        get { return _currentWaypoint; }
        set
        {
            if (_currentWaypoint != value)
            {
                _currentWaypoint = value;
                EmitSignalWaypontChanged(_currentWaypoint);
            }
        }
    }

    private Gamepiece _gamepiece;

    public override void _Ready()
    {
        SetProcess(false);
        SetPhysicsProcess(false);

        if (!Engine.IsEditorHint())
        {
            IsActive = true;
            _gamepiece.Arriving += GamepieceOnArriving;
            _gamepiece.Arrived += GamepieceOnArrived;

            Singletons.FieldEvents.InputPaused += value => IsActive = !value;
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        if (_gamepiece is null)
            return ["This object must be a child of a gamepiece!"];

        return [];
    }

    public override void _Notification(int what)
    {
        if (what == NotificationParented)
        {
            _gamepiece = GetParent<Gamepiece>();
            UpdateConfigurationWarnings();
        }
    }

    public void MoveAlongPath(Array<Vector2I> path)
    {
        _movePath = path;
        MoveToNextWaypoint();
    }

    /// <summary>
    /// Set whether or not the controller may exert control over the gamepiece.
    /// There are a number of occasions (such as cutscenes or combat) where gamepieces are inactive.
    /// </summary>
    /// <param name="value"></param>
    public void SetIsActive(bool value)
    {
        _isActive = value;
        MoveToNextWaypoint();
    }

    public float MoveToNextWaypoint()
    {
        var distanceToPoint = 0f;

        if (_isActive)
        {
            if (_movePath.Count >= 1 && Singletons.Gameboard.Pathfinder.CanMoveTo(_movePath[0]))
            {
                _currentWaypoint = _movePath[0];
                var destination = Singletons.Gameboard.CellToPixel(_currentWaypoint);
                
                // Report how far away the waypoint is.
                distanceToPoint = _gamepiece.Position.DistanceTo(destination);
                _gamepiece.MoveTo(Singletons.Gameboard.CellToPixel(_currentWaypoint));
                
                Singletons.GamepieceRegistry.MoveGamepiece(_gamepiece, _currentWaypoint);
            }
        }

        return distanceToPoint;
    }

    /// <summary>
    /// The controller's gamepiece will finish travelling this frame unless it is extended. When following
    /// a path, the gamepiece will want to travel to the next waypoint.
    /// excess_distance covers cases where the gamepiece will move past the current waypoint and prevents
    /// stuttering for a single frame (or slower-than-expected movement for *very* fast gamepieces).
    /// </summary>
    /// <param name="excessDistance"></param>
    private void GamepieceOnArriving(float excessDistance)
    {
        if (_movePath.Count > 0 && _isActive)
        {
            // Fast gamepieces could jump several waypoints at once, so check to see which waypoint is next in line.
            while (_movePath.Count > 0 && excessDistance > 0)
            {
                if (Singletons.Gameboard.Pathfinder.CanMoveTo(_movePath[0]))
                {
                    return;
                }

                var distanceToWaypoint = MoveToNextWaypoint();
                excessDistance -= distanceToWaypoint;
            }
        }
    }
    
    private void GamepieceOnArrived()
    {
        _movePath.Clear();
    }
}
