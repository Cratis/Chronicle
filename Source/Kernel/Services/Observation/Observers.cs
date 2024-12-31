// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/>.</param>
public class Observers(IStorage storage) : IObservers
{
    /// <inheritdoc/>
    public Task RetryPartition(RetryPartitionRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Rewind(RewindRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task RewindPartition(RewindPartitionRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObservable<ObserverInformation> ObserveObservers(AllObserversRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> GetFailedPartitionsForObserver(FailedPartitionsForObserverRequest request, CallContext context = default)
    {
        var failedPartitions = await storage.GetEventStore(request.EventStoreName).GetNamespace(request.Namespace).FailedPartitions.GetFor(request.ObserverId);
        return failedPartitions.Partitions.Select(_ => _.ToContract()).ToArray();
    }
}
