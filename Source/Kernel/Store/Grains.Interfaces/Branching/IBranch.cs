// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Branching;

/// <summary>
/// Defines a grain representing a specific branch.
/// </summary>
public interface IBranch : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Perform a merge from the branch into the event log.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Merge();
}
