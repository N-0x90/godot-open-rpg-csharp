using Godot;
using System;
using OpenRPG.Field.Cutscenes;
using OpenRPG.Field.Gamepieces;

namespace OpenRPG.Field;

public partial class Field : Node2D
{
    /// <summary>
    /// The cutscene that will play on starting a new game.
    /// </summary>
    [Export] 
    public Cutscene OpeningCutscene;

    /// <summary>
    /// A PlayerController that will be dynamically assigned to whichever Gamepiece the player currently controls.
    /// </summary>
    [Export]
    public PackedScene PlayerController;

    /// <summary>
    /// The first gamepiece that the player will control. This may be null and assigned via an introductory cutscene instead.
    /// </summary>
    [Export]
    public Gamepiece PlayerDefaultGamepiece;

    public override void _Ready()
    {
        GD.Randomize();

        // Assign proper controllers to player gamepieces whenever they change.
        Singletons.Player.GamepieceChanged += PlayerOnGamepieceChanged;

        Singletons.Player.Gamepiece = PlayerDefaultGamepiece;
    }

    private void PlayerOnGamepieceChanged()
    {
        var newGamepiece = Singletons.Player.Gamepiece;
        Singletons.Camera.Gamepiece = newGamepiece;

        // TODO: Fix this
        // foreach (var controller in GetTree().GetNodesInGroup(PlayerController.Group))
        // {
        //     controller.QueueFree();
        // }

        if (newGamepiece is not null)
        {
            // TODO: fix this
            // var newController = (PlayerController)PlayerController.Instantiate();
            //
            // // assert(new_controller is PlayerController, "The Field game state requires a valid PlayerController set in the editor!")            
            //
            // newGamepiece.AddChild(newController);
            // newController.IsActive = true;
        }
    }
}
