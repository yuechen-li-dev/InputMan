using InputMan.Core;
using Microsoft.Xna.Framework;
using System;

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
        // Constants for controlling horizontal movement (moved from Player.cs)
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 1750.0f;
        private const float GroundDragFactor = 0.48f;
        private const float AirDragFactor = 0.58f;

        // Constants for vertical movement
        private const float GravityAcceleration = 3400.0f;
        private const float MaxFallSpeed = 550.0f;

        /// <summary>
        /// Populates the player's input fields for this frame.
        ///
        /// IMPORTANT: This intentionally matches the current behavior in Player.cs:
        /// - movement is updated every frame
        /// - isJumping is only set to true on WasPressed (it is cleared elsewhere)
        /// </summary>
        public void UpdateInput(Player player, IInputMan input)
        {
            player.SetMovement(input.GetAxis(Platformer2DProfile.MoveX));

            if (input.WasPressed(Platformer2DProfile.Jump))
            {
                player.SetJumping(true);
            }
        }

        /// <summary>
        /// Applies horizontal acceleration, drag, and max-speed clamping.
        /// Vertical physics/jump stay in Player for now.
        /// </summary>
        public void ApplyHorizontalPhysics(Player player, float elapsedSeconds)
        {
            var v = player.Velocity;

            // Acceleration from input.
            v.X += player.GetMovement() * MoveAcceleration * elapsedSeconds;

            // Apply pseudo-drag horizontally.
            v.X *= player.IsOnGround ? GroundDragFactor : AirDragFactor;

            // Clamp to max run speed.
            v.X = MathHelper.Clamp(v.X, -MaxMoveSpeed, MaxMoveSpeed);

            player.Velocity = v;
        }

        public void ApplyVerticalPhysics(Player player, float elapsedSeconds, GameTime gameTime)
        {
            var v = player.Velocity;

            v.Y = MathHelper.Clamp(
                v.Y + GravityAcceleration * elapsedSeconds,
                -MaxFallSpeed,
                MaxFallSpeed);

            v.Y = DoJump(player, v.Y, gameTime);

            player.Velocity = v;
        }

        // Jump tuning (moved from Player)
        private const float MaxJumpTime = 0.35f;
        private const float JumpLaunchVelocity = -500.0f;

        // Jump state (moved from Player)
        private float jumpTime;
        private bool wasJumping;

        private float DoJump(Player player, float velocityY, GameTime gameTime)
        {
            bool isJumping = player.GetIsJumping();
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 1. START JUMP: If we just pressed the button and are on the ground.
            if (isJumping && !wasJumping && player.IsOnGround)
            {
                jumpTime = 0.01f; // Setting to a tiny value to flag that a jump is active
                player.PlayJumpSfx();
            }

            // 2. CONTINUE JUMP: If the button is held and we haven't hit the time limit.
            if (isJumping && jumpTime > 0.0f && jumpTime < MaxJumpTime)
            {
                jumpTime += elapsed;
                player.PlayJumpAnimation();

                // Simple linear jump: Apply a constant upward velocity while holding.
                // This replaces the complex Math.Pow logic.
                velocityY = JumpLaunchVelocity;
            }
            else
            {
                // 3. STOP JUMP: If the button is released or we hit the max time.
                jumpTime = 0.0f;
            }

            wasJumping = isJumping;
            return velocityY;
        }
    }
}
