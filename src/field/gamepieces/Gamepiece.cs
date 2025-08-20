using Godot;
using System;
using OpenRPG.Common;
using OpenRPG.Field.Gamepieces.Animation;
using OpenRPG.Gameboards;

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
            
            // TODO: continue here
        }
    }
}
