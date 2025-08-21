using Godot;
using System;
using System.Threading.Tasks;

namespace OpenRPG.Field.Cutscenes;

/// <summary>
/// A cutscene stops field gameplay to run a scripted event.
/// A cutscene may be thought of as the videogame equivalent of a short scene in a film. For example,
/// dialogue may be displayed, the scene may switch to show key NPCs performing an event, or the
/// inventory may be altered. Gameplay on the field is [b]stopped[/b] until the cutscene concludes,
/// though this may span a combat scenario (e.g. epic bossfight).
/// Cutscenes may or may not have duration, and only one cutscene may be active at a time. Field
/// gameplay is stopped for the entire duration of the active cutscene.
/// Gameplay is stopped by emitting the global [signal FieldEvents.input_paused] signal.
/// AI and player objects respond to this signal. For examples or responses to this signal, see
/// [member GamepieceController.is_paused] or [method FieldCursor._on_input_paused].
/// Cutscenes are inherently custom and must be derived to do anything useful. They may be run via
/// the [method run] method and derived cutscenes will override the [method _execute] method to
/// provide custom functionality.
/// Cutscenes are easily extensible, taking advantage of Godot's scene architecture. A variety of
/// cutscene templates are included out-of-the-box. See [Trigger] for a type of cutscene that is
/// triggered by contact with a gamepeiece. See [Interaction] for cutscene's that are triggered by
/// the player interaction with them via a keypress or touch. Several derived temlpates (for example,
/// open-able doors) are included in res://field/cutscenes/templates.
/// </summary>
[Icon("res://assets/editor/icons/Cutscene.svg")]
public partial class Cutscene : Node2D
{
    private bool _isCutsceneInProgress;

    public bool IsCutsceneInProgress
    {
        get { return _isCutsceneInProgress; }
        protected set
        {
            if (_isCutsceneInProgress != value)
            {
                _isCutsceneInProgress = value;
                Singletons.FieldEvents.EmitSignal(FieldEvents.SignalName.InputPaused, value);
            }
        }
    }
    
    public async void Run()
    {
        IsCutsceneInProgress = true;
        
        await Execute();   
        
        IsCutsceneInProgress = false;
    }

    protected virtual async Task Execute()
    {
        
    }
}
