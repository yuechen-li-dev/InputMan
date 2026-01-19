using InputMan.Core;
using InputMan.StrideConn;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace ThirdPersonPlatformerInputManDemo;

public sealed class InstallInputMan : StartupScript
{

    public override void Start()
    {
        var sys = new StrideInputManSystem(Game.Services, DefaultPlatformerProfile.Create());
        //sys.Enabled = true;
        Game.GameSystems.Add(sys);
    }

}
