// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IReplayObserver"/>.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to replay.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="EventTypes">The event types to replay.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.Jobs.ReplayObserverRequest")]
public record ReplayObserverRequest(
    [Id(0)] ObserverKey ObserverKey,
    [Id(1)] ObserverType ObserverType,
    [Id(2)] IEnumerable<EventType> EventTypes) : IObserverJobRequest
{
    /// <inheritdoc/>
    [Id(3)]
    public ObserverReaderScope? ReaderScope { get; init; }

    /// <inheritdoc/>
    [Id(4)]
    public ulong? Revision { get; init; }

    /// <inheritdoc/>
    [Id(5)]
    public ulong? OwnershipEpoch { get; init; }
}
