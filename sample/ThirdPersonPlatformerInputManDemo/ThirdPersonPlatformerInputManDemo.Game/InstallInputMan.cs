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
    private ActionMapId[] InitialMaps { get; set; } =
        [
            new ActionMapId("UI"),
            new ActionMapId("Gameplay")
        ];

    public override void Start()
    {
        Func<InputProfile> buildDefault = DefaultPlatformerProfile.Create;
        var userProfilePath = DemoProfilePaths.GetUserProfilePath();
        var defaultJsonPath = DemoProfilePaths.GetBundledDefaultProfilePath();

        static InputProfile LoadJson(string path) =>
            InputProfileJson.Load(File.ReadAllText(path));

#if DEBUG
        // DEV: code first, then JSON overrides if present
        InputProfile profile;

        if (File.Exists(defaultJsonPath))
            profile = LoadJson(defaultJsonPath);
        else if (File.Exists(userProfilePath))
            profile = LoadJson(userProfilePath);

        profile = buildDefault(); //C#

        //profile = DefaultPlatformerProfileFs.profile; // F#
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

        var sys = new StrideInputManSystem(Game.Services, profile, InitialMaps)
        { Enabled = true };
        Game.GameSystems.Add(sys);
    }
}


