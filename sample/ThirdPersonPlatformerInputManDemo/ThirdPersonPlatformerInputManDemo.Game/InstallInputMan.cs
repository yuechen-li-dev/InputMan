using InputMan.Core;
using InputMan.Core.Serialization;
using InputMan.StrideConn;
using Stride.Engine;
using System;
using System.IO;
using ThirdPersonPlatformerInputManDemoFs;

namespace ThirdPersonPlatformerInputManDemo;

public sealed class InstallInputMan : StartupScript
{

    public override void Start()
    {
        Func<InputProfile> buildDefault = DefaultPlatformerProfileFs.Create; // <- switch line

        var userProfilePath = DemoProfilePaths.GetUserProfilePath();
        var defaultJsonPath = DemoProfilePaths.GetBundledDefaultProfilePath();

        InputProfile profile;

        if (File.Exists(userProfilePath))
        {
            profile = InputProfileJson.Load(File.ReadAllText(userProfilePath));
        }
        else if (File.Exists(defaultJsonPath))
        {
            profile = InputProfileJson.Load(File.ReadAllText(defaultJsonPath));
        }
        else
        {
            // Dev fallback: generate from code if bundled JSON doesn't exist yet
            profile = buildDefault();
        }

        // Seed user profile if missing
        if (!File.Exists(userProfilePath))
        {
            Directory.CreateDirectory(DemoProfilePaths.GetUserProfileDirectory());
            File.WriteAllText(userProfilePath, InputProfileJson.Save(profile));
        }

        var sys = new StrideInputManSystem(Game.Services, profile)
        {
            Enabled = true
        };
        Game.GameSystems.Add(sys);
    }
}


