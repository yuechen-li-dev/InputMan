using InputMan.Core;
using InputMan.Core.Serialization;
using InputMan.StrideConn;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.IO;

namespace ThirdPersonPlatformerInputManDemo;

public sealed class InstallInputMan : StartupScript
{

    public override void Start()
    {
        if (Game.Services.GetService<IInputMan>() != null)
            return;

        var json = File.ReadAllText("Resources/Input/profile.json");
        var profile = InputProfileJson.Load(json);

        var sys = new StrideInputManSystem(Game.Services, profile)
        {
            Enabled = true
        };
        Game.GameSystems.Add(sys);
    }

}
