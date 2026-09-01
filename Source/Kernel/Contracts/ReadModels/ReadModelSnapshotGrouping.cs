// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents how the events that shaped a read model are grouped into snapshots.
/// </summary>
/// <remarks>
/// Correlation is zero so that a request that leaves the field unset groups by correlation, which is
/// what every caller got before there was a choice.
/// </remarks>
public enum ReadModelSnapshotGrouping
{
    /// <summary>
    /// One snapshot per correlation - the events that were applied as a single action.
    /// </summary>
    Correlation = 0,

    /// <summary>
    /// One snapshot per event, so every snapshot moves the read model by exactly one thing that happened.
    /// </summary>
    Event = 1
}
