using InputMan.Core;
using InputMan.Core.Rebind;
using InputMan.MonoGameConn;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Platformer2D.Core
{
    /// <summary>
    /// Simple rebinding UI overlay for Platformer2D.
    /// Shows status messages during rebinding and handles the rebinding flow.
    /// </summary>
    public class RebindingUI
    {
        private readonly IInputMan _inputMan;
        private readonly RebindingManager _rebindManager;
        private readonly SpriteFont _font;

        private string _statusMessage = "";
        private bool _isRebinding = false;

        // Action to rebind when user presses J
        private static readonly ActionId RebindJumpHotkey = new("RebindJump");

        public RebindingUI(IInputMan inputMan, MonoGameProfileStorage storage, SpriteFont font)
        {
            _inputMan = inputMan;
            _font = font;

            // Create rebinding manager
            _rebindManager = new RebindingManager(inputMan, storage);

            // Subscribe to events
            _rebindManager.OnStatusChanged += message =>
            {
                // Sanitize message to ASCII-only (MonoGame fonts may not support Unicode)
                _statusMessage = SanitizeForSpriteFont(message);
            };

            _rebindManager.OnCompleted += success =>
            {
                if (success)
                    _statusMessage = "Jump rebound successfully!";
                else
                    _statusMessage = "Rebinding cancelled.";

                _isRebinding = false;
            };
        }

        public void Update()
        {
            // Check if user wants to start rebinding (press J)
            if (!_isRebinding && _inputMan.WasPressed(RebindJumpHotkey))
            {
                StartRebindJump();
            }
        }

        private void StartRebindJump()
        {
            _isRebinding = true;
            _statusMessage = "Press a key to rebind Jump..."; // Fixed: use plain periods

            // Build candidate buttons (keyboard + gamepad)
            var candidates = MonoGameCandidateButtons.KeyboardAndGamepad(PlayerIndex.One);

            // Forbidden keys (reserved for system functions)
            var forbidden = new HashSet<ControlKey>
            {
                MonoGameKeys.K(Keys.Escape), // Reserved for menu/cancel
            };

            // Start rebinding session
            _rebindManager.StartRebind(
                bindingName: "Jump.Kb.Space", // The binding to rebind
                map: new ActionMapId("Gameplay"),
                candidateButtons: candidates,
                forbiddenControls: forbidden,
                disallowConflicts: true);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 screenSize)
        {
            if (string.IsNullOrEmpty(_statusMessage))
                return;

            // Draw status message at bottom center of screen
            var messageSize = _font.MeasureString(_statusMessage);
            var position = new Vector2(
                (screenSize.X - messageSize.X) / 2,
                screenSize.Y - messageSize.Y - 40); // 40 pixels from bottom

            // Draw shadow
            spriteBatch.DrawString(_font, _statusMessage,
                position + new Vector2(2, 2), Color.Black);

            // Draw text
            var textColor = _isRebinding ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, _statusMessage, position, textColor);
        }

        /// <summary>
        /// Clears the status message (call this after a few seconds or on scene change).
        /// </summary>
        public void ClearMessage()
        {
            if (!_isRebinding)
                _statusMessage = "";
        }

        /// <summary>
        /// Removes non-ASCII characters that MonoGame fonts might not support.
        /// Replaces ellipsis (…) with three periods (...).
        /// </summary>
        private static string SanitizeForSpriteFont(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Replace common Unicode characters with ASCII equivalents
            text = text.Replace("…", "..."); // Ellipsis
            text = text.Replace("–", "-");   // En dash
            text = text.Replace("—", "-");   // Em dash
            text = text.Replace("\"", "\"");  // Smart quotes
            text = text.Replace("\"", "\"");
            text = text.Replace("'", "'");
            text = text.Replace("'", "'");

            // Remove any remaining non-ASCII characters
            var result = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (c < 128) // ASCII only
                    result.Append(c);
            }

            return result.ToString();
        }
    }
}