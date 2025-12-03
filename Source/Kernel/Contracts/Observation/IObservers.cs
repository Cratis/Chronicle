// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Defines the contract for working with observers.
/// </summary>
[Service]
public interface IObservers
{
    /// <summary>
    /// Rewind an observer.
    /// </summary>
    /// <param name="command">The replay command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Replay(Replay command, CallContext context = default);

    /// <summary>
    /// Rewind a partition for an observer.
    /// </summary>
    /// <param name="command">The replay command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task ReplayPartition(ReplayPartition command, CallContext context = default);

    /// <summary>
    /// Retry a failed partition for an observer.
    /// </summary>
    /// <param name="command">The retry command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task RetryPartition(RetryPartition command, CallContext context = default);

    /// <summary>
    /// Get the current details of an observer.
    /// </summary>
    /// <param name="request">The <see cref="GetObserverInformationRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="ObserverInformation"/>.</returns>
    Task<ObserverInformation> GetObserverInformation(GetObserverInformationRequest request, CallContext context = default);

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="request">The <see cref="AllObserversRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="ObserverInformation"/>.</returns>
    [Operation]
    Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default);

    /// <summary>
    /// Observe all observers.
    /// </summary>
    /// <param name="request">The <see cref="AllObserversRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of a collection of <see cref="ObserverInformation"/>.</returns>
    [Operation]
    IObservable<IEnumerable<ObserverInformation>> ObserveObservers(AllObserversRequest request, CallContext context = default);
}
