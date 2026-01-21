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
        Func<InputProfile> buildDefault = DefaultPlatformerProfile.Create; 
        var userProfilePath = DemoProfilePaths.GetUserProfilePath();
        var defaultJsonPath = DemoProfilePaths.GetBundledDefaultProfilePath();

        static InputProfile LoadJson(string path) =>
            InputProfileJson.Load(File.ReadAllText(path));

#if DEBUG
        // DEV: code first, then JSON overrides if present
        
        var profile = buildDefault(); //C#

        // var profile = DefaultPlatformerProfileFs.profile; // F#

        if (File.Exists(defaultJsonPath))
            profile = LoadJson(defaultJsonPath);

        if (File.Exists(userProfilePath))
            profile = LoadJson(userProfilePath);
#else
    // END USER: JSON first, then code fallback
    InputProfile profile;

    if (File.Exists(userProfilePath))
        profile = LoadJson(userProfilePath);
    else if (File.Exists(defaultJsonPath))
        profile = LoadJson(defaultJsonPath);
    else
        profile = buildDefault();
#endif

        // Seed user profile if missing (so rebinding has somewhere writable)
        if (!File.Exists(userProfilePath))
        {
            Directory.CreateDirectory(DemoProfilePaths.GetUserProfileDirectory());
            File.WriteAllText(userProfilePath, InputProfileJson.Save(profile));
        }

        var sys = new StrideInputManSystem(Game.Services, profile) { Enabled = true };
        Game.GameSystems.Add(sys);
    }
}


