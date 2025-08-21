using Godot;
using System.Threading.Tasks;
using Godot.Collections;

namespace OpenRPG.Field.Cutscenes;

/// <summary>
/// A [Cutscene] that is triggered by the presence of the player and the player's input.
/// An active Interaction may be run by the player walking up to it and 'interacting' with it,
/// usually via something as ubiquitous as the spacebar key. Common examples found in most RPGs are
/// NPC conversations, opening treasure chests, activating a save point, etc.
/// An edge case occurs when the player clicks on an interaction, though the player is far away. We
/// only want to report the top-most interaction as being clicked.
/// Interactions handle player input directly and are activated according to the presence of the
/// player's interaction collision shape, which occupies the cell faced by the player's character.
/// </summary>
[Tool]
[Icon("res://assets/editor/icons/Interaction.svg")]
public partial class Interaction : Cutscene
{
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
        UpdateInputState();

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

    /// <summary>
    /// # Track overlapping areas. Determining whether or not to run an event is not as simple as the
    /// presence of overlapping areas, since factors such as gamestate and the existence of another
    /// running events are relevant.
    /// </summary>
    private Array<Area2D> _overlappingAreas = [];

    /// <summary>
    /// A hidden Button control tells us when the player has clicked on the interaction. Note that only
    /// the topmost button may be clicked.
    /// </summary>
    private Button _button;

    public override void _Ready()
    {
        _button = GetNode<Button>("Button");
        
        SetProcessUnhandledInput(false);

        if (!Engine.IsEditorHint())
        {
            Singletons.FieldEvents.InputPaused += FieldEventsOnInputPaused;
            
            // The button does not stop all mouse/touch input, otherwise the user could not highlight
            // selectable cells. Instead, the GUI must prevent only clicks from propogating and should
            // forward the general interaction_selected event.
            _button.Pressed += ButtonOnPressed;
        }
    }

    /// <summary>
    /// Ensure that something is connected to _on_area_entered and _on_area_exited, which the Interaction
    /// requires. If nothing is connected, issue a configuration warning.
    /// </summary>
    /// <returns></returns>
    public override string[] _GetConfigurationWarnings()
    {
        var warnings = new System.Collections.Generic.List<string>();
        var hasAreaEnteredBindings = false;
        var hasAreaExitedBindings = false;

        foreach (var data in GetIncomingConnections())
        {
            var callable = (Callable?)data["callable"];

            if (callable.HasValue && callable.Value.Method == nameof(OnAreaEntered))
            {
                hasAreaEnteredBindings = true;
            }
            else  if (callable.HasValue && callable.Value.Method == nameof(OnAreaExited))
            {
                hasAreaExitedBindings = true;
            }
        }

        if (!hasAreaEnteredBindings)
        {
            warnings.Add("This object does not have a CollisionObject2D's signals connected to this Interactions's _on_area_entered method. The Interaction is not interactable!");
        }

        if (!hasAreaExitedBindings)
        {
            warnings.Add("This object does not have a CollisionObject2D's signals connected to this Interactions's _on_area_exited method. The Interaction can never turn off!");
        }
        
        return warnings.ToArray();
    }

    /// <summary>
    /// An Interaction only processes input when its childrens' collision shape(s) have collided with the
    /// player's interaction collision shape.
    /// </summary>
    /// <param name="event"></param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionReleased("interact"))
            Run();
    }

    private void ButtonOnPressed()
    {
        // Stop the click event from being passed to _unhandled_input.
        _button.AcceptEvent();
        Singletons.FieldEvents.EmitSignal(FieldEvents.SignalName.InteractionSelected, this);
    }

    /// <summary>
    /// Pause any collision objects that would normally send signals regarding interactions.
    /// This will automatically accept or ignore currently overlapping areas.
    /// </summary>
    /// <param name="isPaused"></param>
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

    /// <summary>
    /// Find the entering player interaction shape and enable input, if the interaction is active.
    /// </summary>
    /// <param name="area"></param>
    private void OnAreaEntered(Area2D area)
    {
        if (!Engine.IsEditorHint())
        {
            if (area is not null && _overlappingAreas is not null)
            {
                _overlappingAreas.Add(area);
            }
            
            UpdateInputState();
        }
    }
    
    /// <summary>
    /// Determine whether or not an Interaction is runnable, due to the presence of the player's
    /// interaction shape and whether or not the Interaction is active.
    /// </summary>
    private void UpdateInputState()
    {
        var isRunnable = _isActive && _overlappingAreas.Count > 0;
        SetProcessUnhandledInput(isRunnable);
    }

    /// <summary>
    /// Clean up any references to the player's interaction collision shape.
    /// </summary>
    /// <param name="area"></param>
    public void OnAreaExited(Area2D area)
    {
        if (!Engine.IsEditorHint())
        {
            _overlappingAreas.Remove(area);
            UpdateInputState();
        }
    }
}
