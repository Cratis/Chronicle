// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.EventSequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Keeps kernel observer operations while refusing completion of observers that never run in-process.
/// </summary>
/// <param name="observers">The kernel observer service.</param>
internal sealed class ObserversForTesting(IObservers observers) : IObservers
{
    /// <inheritdoc/>
    public Task<ReplayResponse> Replay(Replay command, CallContext context = default) => observers.Replay(command, context);

    /// <inheritdoc/>
    public Task ReplayPartition(ReplayPartition command, CallContext context = default) => observers.ReplayPartition(command, context);

    /// <inheritdoc/>
    public Task<RetryPartitionResponse> RetryPartition(RetryPartition command, CallContext context = default) => observers.RetryPartition(command, context);

    /// <inheritdoc/>
    public Task<ClearPartitionQuarantineResponse> ClearPartitionQuarantine(ClearPartitionQuarantine command, CallContext context = default) => observers.ClearPartitionQuarantine(command, context);

    /// <inheritdoc/>
    public Task ClearObserverQuarantine(ClearObserverQuarantine command, CallContext context = default) => observers.ClearObserverQuarantine(command, context);

    /// <inheritdoc/>
    public Task ClearFailedPartitions(ClearFailedPartitions command, CallContext context = default) => observers.ClearFailedPartitions(command, context);

    /// <inheritdoc/>
    public Task<RemoveObserverResponse> RemoveObserver(RemoveObserver command, CallContext context = default) => observers.RemoveObserver(command, context);

    /// <inheritdoc/>
    public Task<ObserverInformation> GetObserverInformation(GetObserverInformationRequest request, CallContext context = default) => observers.GetObserverInformation(request, context);

    /// <inheritdoc/>
    public Task<IEnumerable<Contracts.Clients.ConnectedClient>> GetConnectedClientsForObserver(GetConnectedClientsForObserverRequest request, CallContext context = default) => observers.GetConnectedClientsForObserver(request, context);

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default) => observers.GetObservers(request, context);

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverInformation>> ObserveObservers(AllObserversRequest request, CallContext context = default) => observers.ObserveObservers(request, context);

    /// <inheritdoc/>
    public Task<WaitForObserverCompletionResponse> WaitForCompletion(WaitForObserverCompletionRequest request, CallContext context = default) =>
        throw new Observation.CannotWaitForObserverCompletion(new EventStoreName(request.EventStore), new EventSequenceId(request.EventSequenceId));

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverInformation>> GetReplayableObserversForEventTypes(GetReplayableObserversForEventTypesRequest request, CallContext context = default) => observers.GetReplayableObserversForEventTypes(request, context);
}
