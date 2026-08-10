// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Extensions for creating event-seeding buffers for an event store.
/// </summary>
public static class EventStoreSeedingExtensions
{
    /// <summary>
    /// Creates a new empty event-seeding buffer that uses the event store's existing connection and event types.
    /// </summary>
    /// <param name="eventStore">The event store to create a seeding buffer for.</param>
    /// <returns>A new empty <see cref="IEventSeeding"/> buffer.</returns>
    /// <exception cref="CannotCreateEventSeeding">Thrown when the event store does not use Chronicle's event-seeding implementation.</exception>
    /// <remarks>
    /// The buffer exposed by <see cref="IEventStore.Seeding"/> retains its entries when registration fails so the
    /// same idempotent definitions can be retried. Use this method when the definitions themselves have been
    /// corrected and must be offered independently of that retained batch.
    /// </remarks>
    public static IEventSeeding CreateEventSeeding(this IEventStore eventStore) =>
        eventStore.Seeding is EventSeeding seeding && seeding.GetType() == typeof(EventSeeding)
            ? seeding.CreateEmpty()
            : throw new CannotCreateEventSeeding(eventStore.GetType());
}
