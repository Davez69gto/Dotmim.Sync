﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Dotmim.Sync.Enumerations
{
    /// <summary>
    /// Gets the objects we want to provision or deprovision
    /// </summary>
    [Flags]
    public enum SyncProvision
    {
        None = 0,
        Table = 1,
        TrackingTable = 2,
        StoredProcedures = 4,
        Triggers = 8,
        ClientScope = 16,
        ServerScope = 32,
        ServerHistoryScope = 64,
    }
}
