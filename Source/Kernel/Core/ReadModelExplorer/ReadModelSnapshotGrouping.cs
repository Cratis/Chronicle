// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModelExplorer;

/// <summary>
/// Represents how the events behind a read model instance are grouped into snapshots.
/// </summary>
public enum ReadModelSnapshotGrouping
{
    /// <summary>
    /// One snapshot per correlation - the events appended together produce one state.
    /// </summary>
    Correlation = 0,

    /// <summary>
    /// One snapshot per event - the state after every single event.
    /// </summary>
    Event = 1
}
