using InputMan.Core;
using InputMan.Core.Serialization;
using InputMan.StrideConn;
using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.IO;

namespace ThirdPersonPlatformerInputManDemo;

public sealed class InstallInputMan : StartupScript
{

    public override void Start()
    {
       // Func<InputProfile> buildDefault = DefaultPlatformerProfile.Create; // <- switch line

        Func<InputProfile> buildDefault = DefaultPlatformerProfile.Create;

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


