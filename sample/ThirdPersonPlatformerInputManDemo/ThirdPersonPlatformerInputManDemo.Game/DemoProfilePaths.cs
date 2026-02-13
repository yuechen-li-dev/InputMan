using System;
using System.IO;

namespace ThirdPersonPlatformerInputManDemo;

public static class DemoProfilePaths
{
    private const string AppFolderName = "ThirdPersonPlatformerInputManDemo";
    private const string ProfileFileName = "profile.json";

    public static string GetUserProfileDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, AppFolderName);
    }

    public static string GetUserProfilePath()
        => Path.Combine(GetUserProfileDirectory(), ProfileFileName);

    public static string GetBundledDefaultProfilePath()
        => Path.Combine("Resources", "Input", ProfileFileName);
}