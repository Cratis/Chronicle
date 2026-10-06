// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Storage.EventSources;

/// <summary>
/// Defines the storage of registered event source definitions for an event store.
/// </summary>
public interface IEventSourcesStorage
{
    /// <summary>
    /// Gets all registered event source definitions.
    /// </summary>
    /// <returns>A collection of <see cref="EventSourceDefinition"/>.</returns>
    Task<IEnumerable<EventSourceDefinition>> GetAll();

    /// <summary>
    /// Observe all registered event source definitions.
    /// </summary>
    /// <returns>A subject emitting the full collection whenever it changes.</returns>
    ISubject<IEnumerable<EventSourceDefinition>> ObserveAll();

    /// <summary>
    /// Find a registered event source definition by its name.
    /// </summary>
    /// <param name="name">The <see cref="EventSourceName"/> to find.</param>
    /// <returns>The <see cref="EventSourceDefinition"/> if registered; otherwise null.</returns>
    Task<EventSourceDefinition?> Find(EventSourceName name);

    /// <summary>
    /// Saves a definition, replacing any existing definition with the same name.
    /// </summary>
    /// <param name="definition">The <see cref="EventSourceDefinition"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Definitions are never deleted because stored events refer to them.
    /// </remarks>
    Task Save(EventSourceDefinition definition);
}
