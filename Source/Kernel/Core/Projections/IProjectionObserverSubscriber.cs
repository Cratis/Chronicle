// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a specialized <see cref="IObserverSubscriber"/> for projections whose read model key always resolves to
/// the event's own event source id.
/// </summary>
/// <remarks>
/// Every event source targets its own read model document, so a subscriber activation per partition is safe and
/// each activation prefers the silo of its caller. A projection whose key can collapse several event sources
/// onto one document subscribes as <see cref="ICollapsingProjectionObserverSubscriber"/> instead.
/// </remarks>
[Version(1)]
public interface IProjectionObserverSubscriber : IObserverSubscriber, IAmOwnedByKernel
{
    /// <summary>
    /// Releases the activation after a partition job step so the next delivery can be placed next to its caller.
    /// </summary>
    /// <returns>The task representing the release request.</returns>
    Task ReleaseAfterPartition();
}
