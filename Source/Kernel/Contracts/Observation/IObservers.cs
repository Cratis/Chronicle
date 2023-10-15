// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Aksio.Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Defines the contract for working with observers.
/// </summary>
[Service]
public interface IObservers
{
    /// <summary>
    /// Rewind an observer.
    /// </summary>
    /// <param name="request">The rewind request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task Rewind(RewindRequest request, CallContext context = default);

    /// <summary>
    /// Rewind a partition for an observer.
    /// </summary>
    /// <param name="request">The rewind request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task RewindPartition(RewindPartitionRequest request, CallContext context = default);

    /// <summary>
    /// Retry a failed partition for an observer.
    /// </summary>
    /// <param name="request">The retry request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task RetryPartition(RetryPartitionRequest request, CallContext context = default);

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="request">The <see cref="AllObserversRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of <see cref="ObserverInformation"/>.</returns>
    [Operation]
    IEnumerable<ObserverInformation> GetObservers(AllObserversRequest request, CallContext context = default);

    /// <summary>
    /// Observe all observers.
    /// </summary>
    /// <param name="request">The <see cref="AllObserversRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of <see cref="ObserverInformation"/>.</returns>
    [Operation]
    IObservable<ObserverInformation> AllObservers(AllObserversRequest request, CallContext context = default);
}
