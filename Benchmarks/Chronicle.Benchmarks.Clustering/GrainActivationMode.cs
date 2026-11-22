// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// The lifecycle preceding a single grain call.
/// </summary>
public enum GrainActivationMode
{
    /// <summary>An already active grain.</summary>
    Warm = 0,

    /// <summary>A target explicitly deactivated before the call.</summary>
    Cold = 1,

    /// <summary>A replacement silo, retaining the same MongoDB data.</summary>
    PostRestart = 2
}
