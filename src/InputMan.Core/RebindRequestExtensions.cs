using System.Collections.Generic;

namespace InputMan.Core;

public static class RebindRequestExtensions
{
    public static RebindRequest WithForbidden(this RebindRequest req, IReadOnlySet<ControlKey> forbidden)
    {
        req.ForbiddenControls = forbidden;
        return req;
    }

    public static RebindRequest WithAllowedDevices(this RebindRequest req, IReadOnlySet<DeviceKind> allowed)
    {
        req.AllowedDevices = allowed;
        return req;
    }

    public static RebindRequest WithNoConflicts(this RebindRequest req, bool disallowConflicts = true)
    {
        req.DisallowConflictsInSameMap = disallowConflicts;
        return req;
    }
}
