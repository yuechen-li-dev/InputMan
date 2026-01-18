using InputMan.Core;
using InputMan.StrideConn;
using Stride.Engine;

namespace ThirdPersonPlatformerInputManDemo;

public sealed class InstallInputMan : StartupScript
{
    public override void Start()
    {
        if (Game.Services.GetService<IInputMan>() != null)
            return;

        var profile = DefaultPlatformerProfile.Create();

        // Install InputMan system directly (works with IGame)
        var sys = new StrideInputManSystem(Game.Services, profile);
        Game.GameSystems.Add(sys);
    }
}