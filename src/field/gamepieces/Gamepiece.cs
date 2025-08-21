using Godot;
using System;
using OpenRPG.Common;
using OpenRPG.Field.Gamepieces.Animation;
using OpenRPG.Field.Gameboards;

namespace OpenRPG.Field.Gamepieces;

/// <summary>
/// A Gamepiece is a scene that moves about and is snapped to the gameboard.
/// Gamepieces, like other scenes in Godot, are expected to be composed of other nodes (i.e. an AI
/// [GamepieceController], a collision shape, or a [Sprite2D], for example). Gamepieces themselves
/// are 'dumb' objects that do nothing but occupy and move about the gameboard.
/// [br][br][b]Note:[/b] The [code]gameboard[/code] is considered to be the playable area on which a
/// Gamepiece may be placed. The gameboard is made up of cells, each of which may be occupied by one
/// or more gamepieces.
/// </summary>
[Tool]
[Icon("res://assets/editor/icons/Gamepiece.svg")]
public partial class Gamepiece : Path2D
{
    /// <summary>
    /// ## Emitted when a gamepiece is about to finish travlling to its destination cell. The remaining
    /// distance that the gamepiece could travel is based on how far the gamepiece has travelled this
    /// frame. [br][br]
    /// The signal is emitted prior to wrapping up the path and traveller, allowing other objects to
    /// extend the move path, if necessary.
    /// </summary>
    [Signal]
    public delegate void ArrivingEventHandler(float remainingDistance);

    /// <summary>
    /// Emitted when the gamepiece has finished travelling to its destination cell.
    /// </summary>
    [Signal]
    public delegate void ArrivedEventHandler();

    /// <summary>
    /// Emitted when the gamepiece's [member direction] changes, usually as it travels about the board.
    /// </summary>
    [Signal]
    public delegate void DirectionChangedEventHandler(Directions.Points newDirection);

    private PackedScene _animationScene;

    /// <summary>
    /// A [GamepieceAnimation] packed scene that will be automatically added to the gamepiece.
    /// Other scene types will not be accepted.
    /// </summary>
    [Export]
    public PackedScene AnimationScene
    {
        get { return _animationScene; }
        set
        {
            _animationScene = value;
            // TODO: complete this section when animation completed
        }
    }

    /// <summary>
    /// The gamepiece will traverse a movement path at [code]move_speed[/code] pixels per second.
    /// Note that extremely high speeds (finish a long path in a single frame) will produce
    /// unexpected results.
    /// </summary>
    [Export]
    public float MoveSpeed = 64;
    
    /// <summary>
    /// The visual representation of the gamepiece, set automatically based on [member animation_scene].
    /// Usually the animation is only changed by the gamepiece itself, though the designer may want to
    /// play different animations sometimes (such as during a cutscene).
    /// </summary>
    private GamepieceAnimation _animation;

    private Directions.Points _direction;

    public Directions.Points Direction
    {
        get { return _direction; }
        set
        {
            if (value == _direction)
                return;
            
            _direction = value;

            if (!IsInsideTree())
            {
                CallDeferred(nameof(DelayedSetDirection));
                return;
            }
            
            DelayedSetDirection();
        }
    }

    private void DelayedSetDirection()
    {
        _animation.Direction = _direction;
        EmitSignalDirectionChanged(_direction);
    }
    
    public Vector2 RestPosition { get; set; } =  Vector2.Zero;
    
    public Vector2 Destination { get; set; }

    public RemoteTransform2D AnimationTransform { get; set; }

    public PathFollow2D Follower { get; set; }
    
    public override async void _Ready()
    {
        AnimationTransform = GetNode<RemoteTransform2D>("PathFollow2D/CameraAnchor");
        Follower = GetNode<PathFollow2D>("PathFollow2D");
        
        SetProcess(false);

        if (!Engine.IsEditorHint() && IsInsideTree())
        {
            if (Singletons.Gameboard.Properties is null)
                await ToSignal(this, Gameboard.SignalName.PropertiesSet);

            var cell = Singletons.Gameboard.GetCellUnderNode(this);
            Position = Singletons.Gameboard.CellToPixel(cell);
            
            // Then register the gamepiece with the registry. Note that if a gamepiece already exists at
            // the cell, this one will simply be freed.
            if (!Singletons.GamepieceRegistry.Register(this, cell))
            {
                QueueFree();
            }
        }
    }

    public override void _Process(double delta)
    {
        // How far will the gamepiece move this frame
        var moveDistance = MoveSpeed *  delta;

        // We need to let others know that the gamepiece will arrive at the end of its path THIS frame.
        // A controller may want to extend the path (for example, if a move key is held down or if
        // another waypoint should be added to the move path).
        // If we do NOT do so and the path is extended post arrival, there will be a single frame where
        // the gamepiece's velocity is discontinuous (drops, then increases again), causing jittery
        // movement.
        // The excess travel distance allows us to know how much to extend the path by. A VERY fast
        // gamepiece may jump a few cells at a time.
        var excessTravelDistance = Follower.Progress + moveDistance - Curve.GetBakedLength();
        if (excessTravelDistance >= 0)
        {
            EmitSignalArriving((float)excessTravelDistance);
        }
        
        // The path may have been extended, so the gamepiece can move along the path now.
        Follower.Progress += (float)moveDistance;

        // Figure out which direction the gamepiece is facing, making sure that the GamepieceAnimation
        // scene doesn't rotate.
        _animation.GlobalRotation = 0;
        _direction = Directions.AngleToDirection(Follower.Rotation);

        // If the gamepiece has arrived, update it's position and movement details.
        if (Follower.Progress >= Curve.GetBakedLength())
        {
            Stop();
        }
    }

    /// <summary>
    /// Move the gamepiece towards a point, given in pixel coordinates.
    /// If the Gamepiece is currently moving, this point will be added to the current path (see
    /// [member Path2D.curve]. Otherwise, a new curve is created with the point as the target.[br][br]
    /// Note that the Gamepiece's position will remain fixed until it has fully traveresed its movement
    /// path. At this point, its position is then updated to its destination.
    /// </summary>
    /// <param name="targetPoint"></param>
    public void MoveTo(Vector2 targetPoint)
    {
        // Note that the destination is where the gamepiece will end up in game world coordinates.
        Destination = targetPoint;
        SetProcess(true);

        if (Curve is null)
        {
            Curve = new Curve2D();
            Curve.AddPoint(Vector2.Zero);
            _animation.Play("run");
        }
        
        // The positions on the path, however, are all relative to the gamepiece's current position. The
        // position doesn't update until the Gamepiece reaches its final destination, otherwise the path
        // would move along with the gamepiece.
        Curve.AddPoint(Destination - Position);
    }

    /// <summary>
    /// Stop the gamepiece from travelling and update its positio
    /// </summary>
    public void Stop()
    {
        // Sort out gamepiece position, resetting the follower and placing everything at the destination.
        Position = Destination;
        Follower.Progress = 0;
        Curve = null;
        Destination = Vector2.Zero;

        // Handle the change to animation.
        _animation.GlobalRotation = 0;
        _animation.Play("idle");
        
        // Stop movement and update logic.
        SetProcess(false);
        EmitSignalArrived();
    }

    /// <summary>
    /// Returns [code]true[/code] if the gamepiece is currently moving along its [member Path2D.curve].
    /// </summary>
    /// <returns></returns>
    public bool IsMoving()
    {
        return IsProcessing();
    }
}
