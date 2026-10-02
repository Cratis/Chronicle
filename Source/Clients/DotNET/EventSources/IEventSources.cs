// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Defines the system for working with the event source definitions of an event store.
/// </summary>
public interface IEventSources
{
    /// <summary>
    /// Gets all discovered event source definitions.
    /// </summary>
    IReadOnlyList<EventSourceDefinition> All { get; }

    /// <summary>
    /// Discover the event source definitions from the client artifacts.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DuplicateEventSourceName">Thrown when two definitions share a name.</exception>
    /// <exception cref="DuplicateEventStreamName">Thrown when a definition declares the same stream twice.</exception>
    Task Discover();

    /// <summary>
    /// Register the discovered definitions with the Kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Get the definition carried by a type.
    /// </summary>
    /// <param name="type">The type of the <see cref="IEventSource"/>.</param>
    /// <returns>The <see cref="EventSourceDefinition"/>.</returns>
    /// <exception cref="UnknownEventSource">Thrown when the type is not a discovered event source.</exception>
    EventSourceDefinition GetFor(Type type);

    /// <summary>
    /// Get the definition with a name.
    /// </summary>
    /// <param name="name">The name of the event source.</param>
    /// <returns>The <see cref="EventSourceDefinition"/>.</returns>
    /// <exception cref="UnknownEventSource">Thrown when there is no event source with the name.</exception>
    EventSourceDefinition GetFor(string name);
}
