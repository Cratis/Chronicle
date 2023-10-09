// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Contracts.Events;
using ProtoBuf.Grpc;

namespace Aksio.Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Defines the contract for working with observers.
/// </summary>
public interface IObservers
{
    IObservable<ObserverEvents> Observe()

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="request">The <see cref="AllObserversRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of <see cref="ObserverInformation"/>.</returns>
    IEnumerable<ObserverInformation> GetObservers(AllObserversRequest request, CallContext context = default);

    /// <summary>
    /// Observe all observers.
    /// </summary>
    /// <param name="request">The <see cref="AllObserversRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of <see cref="ObserverInformation"/>.</returns>
    IObservable<ObserverInformation> AllObservers(AllObserversRequest request, CallContext context = default);
}

public class ObserverEvents
{
    public IEnumerable<AppendedEvent> Events { get; set; }
}
