using Godot;
using System.Collections.Generic;
using OpenRPG.Common;

namespace OpenRPG.Field.Gamepieces.Animation;

/// <summary>
/// Encapsulates [Gamepiece] animation as an optional component.
/// Allows playing animations that automatically adapt to the parent
/// [Gamepiece]'s direction by calling [method play]. Transitions between
/// animations are handled automatically, including changes to direction.
/// [br][br][b]Note:[/b] This is usually not added to the scene tree directly by
/// the designer.
/// Rather, it is typically added to a [Gamepiece] through the [member Gamepiece.animation_scene]
/// property.
/// </summary>
[Tool]
[Icon("res://assets/editor/icons/GamepieceAnimation.svg")]
public partial class GamepieceAnimation : Marker2D
{
    /// <summary>
    /// Name of the animation sequence used to reset animation properties to default.
    /// Note that this animation is only played for a single frame during animation
    /// transitions.
    /// </summary>
    public const string ResetSequenceKey = "RESET";
    
    /// <summary>
    /// Mapping that pairs cardinal [constant Directions.Points] with a [String] suffix.
    /// </summary>
    private static readonly Dictionary<Directions.Points, string> DirectionSuffixes = new()
        {
            { Directions.Points.North, "_n" },
            { Directions.Points.East,  "_e" },
            { Directions.Points.South, "_s" },
            { Directions.Points.West,  "_w" }
        };

    private string _currentSequenceId;

    /// <summary>
    /// The animation currently being played.
    /// </summary>
    [Export]
    public string CurrentSequenceId
    {
        get { return _currentSequenceId; }
        set
        {
            Play(value);
        }
    }

    private Directions.Points _direction = Directions.Points.South;

    /// <summary>
    /// The direction faced by the gamepiece.
    /// Animations may optionally be direction-based. Setting the direction will use
    /// directional animations if they are available; otherwise non-directional
    /// animations will be used.
    /// </summary>
    [Export]
    public Directions.Points Direction
    {
        get { return _direction; }
        set
        {
            SetDirection(value);
        }
    }

    private AnimationPlayer _anim;

    public override void _Ready()
    {
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    /// <summary>
    /// Change the currently playing animation to a new value, if it exists.
    /// Animations may be added with or without a directional suffix (i.e. _n for
    /// north/up). Directional animations will be preferred with direction-less
    /// animations as a fallback.
    /// </summary>
    /// <param name="value"></param>
    public async void Play(string value)
    {
        if (value == _currentSequenceId)
            return;

        if (!IsInsideTree())
        {
            // TODO: Check this!
            await ToSignal(this, Node.SignalName.Ready);
            // CallDeferred(nameof(WaitPlay), value);
            // return;
        }
        
        // We need to check to see if the animation is valid. First of all, look for
        // a directional equivalent - e.g. idle_n. If that fails, look for the new
        // sequence id itself.
        WaitPlay(value);
    }

    private void WaitPlay(string value)
    {
        var sequenceSuffix = DirectionSuffixes[_direction];
        if (_anim.HasAnimation(value + sequenceSuffix))
        {
            CurrentSequenceId = value;
            SwapAnimation(value + sequenceSuffix, false);
        }
        else if (_anim.HasAnimation(value))
        {
            CurrentSequenceId = value;
            SwapAnimation(value, false);
        }
    }
    
    /// <summary>
    /// Change the animation's direction.
    /// If the currently running animation has a directional variant matching the new
    /// direction it will be played. Otherwise, the direction-less animation will
    /// play.
    /// </summary>
    /// <param name="value"></param>
    public async void SetDirection(Directions.Points value)
    {
        if (value == _direction)
            return;
        
        _direction = value;

        if (!IsInsideTree())
        {
            // TODO: Check this!
            await ToSignal(this, Node.SignalName.Ready);
            // CallDeferred(nameof(WaitSetDirection));
            // return;
        }
        
        WaitSetDirection();
    }
    
    private void WaitSetDirection()
    {
        var sequenceSuffix = DirectionSuffixes[_direction];
        if (_anim.HasAnimation(_currentSequenceId + sequenceSuffix))
            SwapAnimation(_currentSequenceId + sequenceSuffix, true);
        else if (_anim.HasAnimation(_currentSequenceId))
            SwapAnimation(_currentSequenceId, true);
    }

    private void SwapAnimation(string nextSequence, bool keepPosition)
    {
        var nextAnim = _anim.GetAnimation(nextSequence);
     
        if (string.IsNullOrWhiteSpace(nextSequence))
            return;

        double currentPositionRatio = 0;

        if (keepPosition)
        {
            currentPositionRatio = _anim.CurrentAnimationPosition / _anim.CurrentAnimationLength;
        }

        if (_anim.HasAnimation(ResetSequenceKey))
        {
            _anim.Play(ResetSequenceKey);
            _anim.Advance(0);
        }
        
        _anim.Play(nextSequence);
        _anim.Advance(currentPositionRatio * nextAnim.Length);
    }
}
