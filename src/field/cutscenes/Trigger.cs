using Godot;
using System;
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
}
