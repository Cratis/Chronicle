// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.Observation.Reactors;

/// <summary>
/// Defines the contract for working with client observers.
/// </summary>
[Service]
public interface IReactors
{
    /// <summary>
    /// Observer an event sequence.
    /// </summary>
    /// <param name="messages">Observable of messages coming from the client.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of <see cref="EventsToObserve"/>.</returns>
    [Operation]
    IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default);
}
