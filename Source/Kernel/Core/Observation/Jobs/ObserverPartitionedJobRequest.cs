// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a request to an observer job.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to catch up.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="Key"><see cref="Key">Partition</see> to retry.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Observation.Jobs.ObserverPartitionedJobRequest")]
public abstract record ObserverPartitionedJobRequest([Id(0)] ObserverKey ObserverKey, [Id(1)] ObserverType ObserverType, [Id(2)] Key Key) : IObserverJobRequest
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

    /// <summary>
    /// Creates an <see cref="ObserverSubscriberKey"/> from the request.
    /// </summary>
    /// <param name="siloAddress">The <see cref="SiloAddress"/>.</param>
    /// <returns>The <see cref="ObserverSubscriberKey"/>.</returns>
    public ObserverSubscriberKey ToObserverSubscriberKey(SiloAddress siloAddress) => new(
                    ObserverKey.ObserverId,
                    ObserverKey.EventStore,
                    ObserverKey.Namespace,
                    ObserverKey.EventSequenceId,
                    Key?.ToString() ?? EventSourceId.Unspecified,
                    siloAddress.ToParsableString());
}
