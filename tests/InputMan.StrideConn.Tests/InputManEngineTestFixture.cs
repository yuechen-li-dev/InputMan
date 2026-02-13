using InputMan.Core;
using Stride.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputMan.StrideConn.Tests
{
    public class InputManEngineTestFixture
    {
        public InputManEngine CreateEngineWithJumpBinding()
        {
            var profile = new InputProfile
            {
                Maps = new Dictionary<string, ActionMapDefinition>
                {
                    ["Gameplay"] = new ActionMapDefinition
                    {
                        Id = new ActionMapId("Gameplay"),
                        Bindings = [Bind.Action(StrideKeys.K(Keys.Space), new ActionId("Jump"))]
                    }
                }
            };
            return new InputManEngine(profile);
        }

        public InputSnapshot CreateSnapshot(params (Keys key, bool down)[] keys)
        {
            var dict = new Dictionary<ControlKey, bool>();
            foreach (var (key, down) in keys)
            {
                if (down)
                    dict[StrideKeys.K(key)] = true;
            }
            return new InputSnapshot(dict, new Dictionary<ControlKey, float>());
        }
    }
}
