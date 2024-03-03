// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Kernel.Contracts.Observation;

/// <summary>
/// Defines the contract for working with client observers.
/// </summary>
[Service]
public interface IClientObservers
{
    /// <summary>
    /// Observer an event sequence.
    /// </summary>
    /// <param name="messages">Observable of messages coming from the client.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of <see cref="EventsToObserve"/>.</returns>
    [Operation]
    IObservable<EventsToObserve> Observe(IObservable<ObserverClientMessage> messages, CallContext context = default);
}
