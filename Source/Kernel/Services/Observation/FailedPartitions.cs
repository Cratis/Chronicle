// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitions"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/>.</param>
internal sealed class FailedPartitions(IStorage storage) : IFailedPartitions
{
    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> GetFailedPartitions(GetFailedPartitionsRequest request, CallContext context = default)
    {
        var failedPartitions = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).FailedPartitions.GetFor(GetObserverId(request.ObserverId));
        return failedPartitions.Partitions.Select(_ => _.ToContract()).ToArray();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<FailedPartition>> ObserveFailedPartitions(GetFailedPartitionsRequest request, CallContext context = default) =>
        storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).FailedPartitions
            .ObserveAllFor(GetObserverId(request.ObserverId))
            .CompletedBy(context.CancellationToken)
            .Select(_ => _.ToContract());

    Concepts.Observation.ObserverId? GetObserverId(string? observerId) => observerId switch
    {
        null => null,
        _ => (Concepts.Observation.ObserverId)observerId
    };
}
