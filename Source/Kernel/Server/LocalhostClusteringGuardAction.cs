// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// The protection selected for Localhost clustering.
/// </summary>
internal enum LocalhostClusteringGuardAction
{
    /// <summary>No storage guard is needed.</summary>
    None = 0,

    /// <summary>Acquire the single-writer lease.</summary>
    Lease = 1,

    /// <summary>Warn about explicitly disabled protection.</summary>
    Warn = 2
}
