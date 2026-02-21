using InputMan.Core;


namespace Platformer2D.Core.Game.Player
{
    /// <summary>
    /// Thin wrapper around input reading for the Player.
    ///
    /// Goal (for now): separate "read input" from the giant Player.cs file,
    /// while keeping the *exact same* behavior as the current refactor.
    /// We'll iterate on feel/controls after this split is stable.
    /// </summary>
    internal sealed class PlayerController
    {
        /// <summary>
        /// Populates the player's input fields for this frame.
        ///
        /// IMPORTANT: This intentionally matches the current behavior in Player.cs:
        /// - movement is updated every frame
        /// - isJumping is only set to true on WasPressed (it is cleared elsewhere)
        /// </summary>
        public void UpdateInput(Player player, IInputMan input)
        {
            // Horizontal movement.
            player.SetMovement(input.GetAxis(Platformer2DProfile.MoveX));

            // Jump "pressed" edge.
            if (input.WasPressed(Platformer2DProfile.Jump))
            {
                player.SetJumping(true);
            }
        }
    }
}
