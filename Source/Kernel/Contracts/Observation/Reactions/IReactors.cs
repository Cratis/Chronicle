// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    /// <summary>
    /// Check if a reactor exists.
    /// </summary>
    /// <param name="request">The <see cref="HasReactorRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A <see cref="HasReactorResponse"/>.</returns>
    [Operation]
    Task<HasReactorResponse> HasReactor(HasReactorRequest request, CallContext context = default);
}
