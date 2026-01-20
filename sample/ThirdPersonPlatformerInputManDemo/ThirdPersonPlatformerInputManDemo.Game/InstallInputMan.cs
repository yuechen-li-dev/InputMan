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

        var userDir = DemoProfilePaths.GetUserProfileDirectory();
        var userProfilePath = DemoProfilePaths.GetUserProfilePath();
        var defaultProfilePath = DemoProfilePaths.GetBundledDefaultProfilePath();

        InputProfile profile;

        if (File.Exists(userProfilePath))
        {
            var json = File.ReadAllText(userProfilePath);
            profile = InputProfileJson.Load(json);
        }
        else
        {
            var json = File.ReadAllText(defaultProfilePath);
            profile = InputProfileJson.Load(json);

            // Optional: seed user profile so rebinding can save immediately
            Directory.CreateDirectory(userDir);
            File.WriteAllText(userProfilePath, InputProfileJson.Save(profile));
        }

        var sys = new StrideInputManSystem(Game.Services, profile)
        {
            Enabled = true
        };
        Game.GameSystems.Add(sys);
    }
}


