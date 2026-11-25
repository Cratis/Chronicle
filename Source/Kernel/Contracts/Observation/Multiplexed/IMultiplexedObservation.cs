// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Multiplexed;

/// <summary>
/// Defines the contract for observing with every reactor and reducer of one client connection over a single stream.
/// </summary>
/// <remarks>
/// <para>
/// The per-observer <c language="csharp">Reactors.Observe</c> and <c language="csharp">Reducers.Observe</c> operations stay and behave exactly as before.
/// This operation carries the same messages, tagged with a stream identifier chosen by the client, so one HTTP/2
/// stream replaces one stream per observer. A client uses it only when the kernel advertises
/// <see cref="Clients.KernelCapabilities.MultiplexedObservation"/>.
/// </para>
/// <para>
/// Flow control is the delivery acknowledgment the observer protocol already has: the kernel hands an observer a
/// delivery and does not hand it the next one for the same partition until the client answered, so no observer can have
/// more than its own bounded set of unanswered deliveries in flight, and a slow observer cannot starve the others.
/// </para>
/// </remarks>
[Service]
public interface IMultiplexedObservation
{
    /// <summary>
    /// Observe with many reactors and reducers over one stream.
    /// </summary>
    /// <param name="messages">Observable of the messages coming from the client, each carrying the messages of several streams.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of messages for the client, each belonging to one stream.</returns>
    [Operation]
    IObservable<MultiplexedServerMessage> Observe(IObservable<MultiplexedClientMessage> messages, CallContext context = default);
}
