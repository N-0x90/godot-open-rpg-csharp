using Godot;
using System;
using System.Threading.Tasks;
using OpenRPG.Field.Gamepieces;

namespace OpenRPG.Field.Cutscenes;

/// <summary>
/// A [Cutscene] that triggers on collision with a [Gamepiece]'s collision shapes.
/// A Gamepiece with collision shapes on a layer monitored by the Trigger may activate the Trigger.
/// Triggers typically wait for [signal Gamepiece.arriving] before being run, but that behaviour may
/// be overridden in derived Triggers by modifying [method _on_area_entered].
/// </summary>
[Tool]
[Icon("res://assets/editor/icons/Contact.svg")]
public partial class Trigger : Cutscene
{
    /// <summary>
    /// Emitted when a [Gamepiece] begins moving to the cell occupied by the [code]Trigger[/code].
    /// </summary>
    [Signal]
    public delegate void GamepieceEnteredEventHandler(Gamepiece gamepiece);

    /// <summary>
    /// Emitted when a [Gamepiece] begins moving away from the cell occupied by the [code]Trigger[/code].
    /// </summary>
    [Signal]
    public delegate void GamepieceExitedEventHandler(Gamepiece gamepiece);

    /// <summary>
    /// Emitted when a [Gamepiece] is finishing moving to the cell occupied by the [code]Trigger[/code].
    /// </summary>
    [Signal]
    public delegate void TriggeredEventHandler(Gamepiece gamepiece);
    
        private bool _isActive;

    /// <summary>
    /// An active [code]Interaction[/code] may be run, whereas one that is inactive may only be run
    /// directly through code via the [method Cutscene.run] method.
    /// </summary>
    [Export]
    public bool IsActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;

            if (!Engine.IsEditorHint())
            {
                // WARNING: this is a "complex" function, need more work!
                _ = UpdateInputStateAsync();
            }
        }
    }
    
    private async Task UpdateInputStateAsync()
    {
        if (!IsInsideTree())
            await ToSignal(this, Node.SignalName.Ready);

        // We use "Visible Collision Shapes" to debug positions on the gameboard, so we'll want 
        // to change the state of child collision shapes.These could be either CollisionShape2Ds
        // or CollisionPolygon2Ds.
        // Note that we only want to disable the collision shapes of objects that are actually
        // connected to this Interactio
        foreach (var data in GetIncomingConnections())
        {
            var callable = (Callable?)data["callable"];

            if (callable.HasValue && callable.Value.Method == nameof(OnAreaEntered))
            {
                var connectedArea = ((Signal)data["signal"]).Owner as Area2D;
                if (connectedArea != null)
                {
                    foreach (var node in connectedArea.FindChildren("*", "CollisionShape2D"))
                    {
                        if (node is CollisionShape2D shape)
                            shape.Disabled = !_isActive;
                    }
                    foreach (var node in connectedArea.FindChildren("*", "CollisionPolygon2D"))
                    {
                        if (node is CollisionPolygon2D polygon)
                            polygon.Disabled = !_isActive;
                    }
                }
            }
        }
    }

    public override void _Ready()
    {
        if (!Engine.IsEditorHint())
            Singletons.FieldEvents.InputPaused += FieldEventsOnInputPaused;
    }

    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        var hasAreaEnteredBindings = false;
        
        foreach (var data in GetIncomingConnections())
        {
            var callable = (Callable?)data["callable"];
            if (callable.HasValue && callable.Value.Method == nameof(OnAreaEntered))
            {
                hasAreaEnteredBindings = true;
            }
        }

        if (!hasAreaEnteredBindings)
        {
            warnings.Add("This object does not have a CollisionObject2D's signals connected to this Trigger's _on_area_entered method. The Trigger will never be triggered!");
        }
        
        return warnings.ToArray();
    }

    private void FieldEventsOnInputPaused(bool isPaused)
    {
        foreach (var data in GetIncomingConnections())
        {
            var callable = (Callable?)data["callable"];

            // Note that we only want to check _on_area_entered, since _on_area_exited will clean up any
            // lingering references once the Area2Ds are 'shut off' (i.e. not monitoring/monitorable).
            if (callable.HasValue && callable.Value.Method == nameof(OnAreaEntered))
            {
                var connectedArea = ((Signal)data["signal"]).Owner as Area2D;
                if (connectedArea != null)
                {
                    connectedArea.Monitoring = !isPaused;
                    connectedArea.Monitorable = !isPaused;
                }
            }
        }
    }

    public async void OnAreaEntered(Area2D area)
    {
        var gamepiece = (Gamepiece?)area.GetParent();

        if (gamepiece != null && gamepiece.IsMoving())
        {
            EmitSignalGamepieceEntered(gamepiece);
            
            void Handler()
            {
                OnGamepieceArrived(gamepiece);
                gamepiece.Arrived -= Handler;
            }
            
            gamepiece.Arrived += Handler;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            IsCutsceneInProgress = true;
        }
    }

    private void OnAreaExited(Area2D area)
    {
        var gamepiece = (Gamepiece?)area.GetParent();
        if (gamepiece != null)
            EmitSignalGamepieceExited(gamepiece);
    }

    private void OnGamepieceArrived(Gamepiece gamepiece)
    {
        EmitSignalTriggered(gamepiece);
        Run();
    }
}
