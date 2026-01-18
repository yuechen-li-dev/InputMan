using InputMan.Core;
using InputMan.StrideConn;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace ThirdPersonPlatformerInputManDemo;

public sealed class InstallInputMan : SyncScript
{

    public override void Start()
    {
        var sys = new StrideInputManSystem(Game.Services, DefaultPlatformerProfile.Create());
        //sys.Enabled = true;
        Game.GameSystems.Add(sys);
    }

    public override void Update()
    {
        DebugText.Print($"Systems count: {Game.GameSystems.Count}", new Int2(10, 10));
    }

}
