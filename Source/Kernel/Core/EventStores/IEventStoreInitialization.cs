// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Ensures event stores are initialized before they are listed or read.
/// </summary>
public interface IEventStoreInitialization
{
    /// <summary>
    /// Ensures an event store is initialized.
    /// </summary>
    /// <param name="eventStore">The event store to initialize.</param>
    /// <returns>A task representing the initialization.</returns>
    Task Ensure(EventStoreName eventStore);

    /// <summary>
    /// Ensures event stores are initialized, leaving out stores whose initialization fails.
    /// </summary>
    /// <param name="eventStores">The event stores to initialize.</param>
    /// <returns>The successfully initialized event stores in input order.</returns>
    Task<IEnumerable<EventStoreName>> EnsureAll(IEnumerable<EventStoreName> eventStores);

    /// <summary>
    /// Forgets cached initialization state after a kernel state reset.
    /// </summary>
    void Clear();
}
