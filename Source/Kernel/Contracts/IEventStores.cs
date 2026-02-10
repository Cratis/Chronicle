// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Defines the contract for working with event stores.
/// </summary>
[Service]
public interface IEventStores
{
    /// <summary>
    /// Get all available event stores.
    /// </summary>
    /// <returns>Collection of strings representing the names of the event stores.</returns>
    [Operation]
    Task<IEnumerable<string>> GetEventStores();

    /// <summary>
    /// Observe all available event stores.
    /// </summary>
    /// <param name="callContext">gRPC call context.</param>
    /// <returns>Observable of all event stores.</returns>
    [Operation]
    IObservable<IEnumerable<string>> ObserveEventStores(CallContext callContext = default);

    /// <summary>
    /// Ensure an event store exists.
    /// </summary>
    /// <param name="command">The ensure command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Ensure(EnsureEventStore command);

    /// <summary>
    /// Set the domain specification for an event store.
    /// </summary>
    /// <param name="command">The domain specification command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task SetDomainSpecification(SetEventStoreDomainSpecification command);
}
