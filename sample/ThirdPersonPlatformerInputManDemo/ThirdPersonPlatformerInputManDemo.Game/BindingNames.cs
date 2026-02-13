using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPersonPlatformerInputManDemo
{
    /// <summary>
    /// Centralized binding name constants.
    /// Matches the names in DefaultPlatformerProfile.
    /// </summary>
    public static class BindingNames
    {
        // Gameplay - Keyboard
        public const string JumpKeyboard = "Jump.Kb";
        public const string MoveLeftKeyboard = "MoveLeft.Kb";
        public const string MoveRightKeyboard = "MoveRight.Kb";
        public const string MoveForwardKeyboard = "MoveFwd.Kb";
        public const string MoveBackKeyboard = "MoveBack.Kb";

        // Gameplay - Mouse
        public const string LookLockMouse = "LookLock.Mouse";
        public const string LookUnlockKeyboard = "LookUnlock.Kb";

        // UI
        public const string PauseKeyboard1 = "Pause.Kb.1";
        public const string PauseKeyboard2 = "Pause.Kb.2";
    }
}
