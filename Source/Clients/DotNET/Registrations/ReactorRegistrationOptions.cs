// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents immutable options for an explicitly registered reactor.
/// </summary>
public sealed class ReactorRegistrationOptions
{
    ReactorRegistrationOptions(EventStoreName eventStore) => EventStore = eventStore;

    /// <summary>
    /// Gets the source event store for the reactor.
    /// </summary>
    public EventStoreName? EventStore { get; }

    /// <summary>
    /// Creates options that observe the inbox for the supplied source event store.
    /// </summary>
    /// <param name="eventStore">The source event store name.</param>
    /// <returns>The immutable reactor registration options.</returns>
    /// <exception cref="ArgumentException">The event store name is null, empty, or whitespace.</exception>
    public static ReactorRegistrationOptions FromEventStore(EventStoreName eventStore)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventStore?.Value);
        return new(eventStore);
    }
}
