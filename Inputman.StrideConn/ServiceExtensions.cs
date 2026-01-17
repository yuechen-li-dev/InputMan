using InputMan.Core;
using Stride.Engine;

namespace InputMan.StrideConn;

public static class InputManStrideExtensions
{
    public static void AddInputMan(this Game game, InputProfile profile)
    {
        // Only add once
        if (game.Services.GetService<IInputMan>() != null)
            return;

        var sys = new StrideInputManSystem(game.Services, profile);
        game.GameSystems.Add(sys);
    }
}
