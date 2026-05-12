// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Provides assertion extension methods for <see cref="IEventSequence"/> and <see cref="IEventLog"/>.
/// </summary>
public static class EventSequenceShouldExtensions
{
    /// <summary>
    /// Asserts that the tail sequence number of the event sequence matches the expected value.
    /// </summary>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="expected">The expected tail <see cref="EventSequenceNumber"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when the tail sequence number does not match.</exception>
    public static async Task ShouldHaveTailSequenceNumber(this IEventSequence eventSequence, EventSequenceNumber expected)
    {
        var actual = await eventSequence.GetTailSequenceNumber();

        if (actual != expected)
        {
            throw new EventSequenceAssertionException(
                $"Expected tail sequence number to be {expected}, but it was {actual}.");
        }
    }

    /// <summary>
    /// Asserts that at least one event of the specified type was appended to the event sequence.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when no event of the expected type is found.</exception>
    public static Task ShouldHaveAppendedEvent<TEvent>(this IEventSequence eventSequence) =>
        eventSequence.ShouldHaveAppendedEvent<TEvent>(_ => { });

    /// <summary>
    /// Asserts that at least one event of the specified type was appended and validates its content.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="validator">An action that validates the first matching event instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when no event of the expected type is found or the validator throws.</exception>
    public static async Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        Action<TEvent> validator)
    {
        var events = await eventSequence.GetFromSequenceNumber(EventSequenceNumber.First);
        AssertAnyEventInList(events, validator);
    }

    /// <summary>
    /// Asserts that at least one event of the specified type was appended and matches the predicate.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="predicate">A function that returns true if the event matches expectations.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when no matching event is found.</exception>
    public static async Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        Func<TEvent, bool> predicate)
    {
        var events = await eventSequence.GetFromSequenceNumber(EventSequenceNumber.First);
        AssertAnyEventInList(events, predicate);
    }

    /// <summary>
    /// Asserts that at least one event of the specified type was appended for the given event source.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to filter by.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when no event of the expected type is found.</exception>
    public static Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        EventSourceId eventSourceId) =>
        eventSequence.ShouldHaveAppendedEvent<TEvent>(eventSourceId, _ => { });

    /// <summary>
    /// Asserts that at least one event of the specified type was appended for the given event source and validates its content.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to filter by.</param>
    /// <param name="validator">An action that validates the first matching event instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when no event of the expected type is found or the validator throws.</exception>
    public static async Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        EventSourceId eventSourceId,
        Action<TEvent> validator)
    {
        var events = await eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId);
        AssertAnyEventInList(events, validator);
    }

    /// <summary>
    /// Asserts that at least one event of the specified type was appended for the given event source and matches the predicate.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to filter by.</param>
    /// <param name="predicate">A function that returns true if the event matches expectations.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when no matching event is found.</exception>
    public static async Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        EventSourceId eventSourceId,
        Func<TEvent, bool> predicate)
    {
        var events = await eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId);
        AssertAnyEventInList(events, predicate);
    }

    /// <summary>
    /// Asserts that a specific event type was appended at the given sequence number.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> at which the event is expected.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when the event is not found or has the wrong type.</exception>
    public static Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        EventSequenceNumber sequenceNumber) =>
        eventSequence.ShouldHaveAppendedEvent<TEvent>(sequenceNumber, _ => { });

    /// <summary>
    /// Asserts that a specific event type was appended at the given sequence number and validates its content.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> at which the event is expected.</param>
    /// <param name="validator">An action that validates the specific event instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when the event is not found, has the wrong type, or the validator throws.</exception>
    public static async Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        EventSequenceNumber sequenceNumber,
        Action<TEvent> validator)
    {
        var events = await eventSequence.GetFromSequenceNumber(sequenceNumber);
        AssertEventAtSequenceNumber(events, sequenceNumber, validator);
    }

    /// <summary>
    /// Asserts that a specific event type was appended at the given sequence number and matches the predicate.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> at which the event is expected.</param>
    /// <param name="predicate">A function that returns true if the event matches expectations.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when the event is not found, has the wrong type, or the predicate returns false.</exception>
    public static async Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        EventSequenceNumber sequenceNumber,
        Func<TEvent, bool> predicate)
    {
        var events = await eventSequence.GetFromSequenceNumber(sequenceNumber);
        AssertEventAtSequenceNumber(events, sequenceNumber, predicate);
    }

    /// <summary>
    /// Asserts that a specific event type was appended for the given event source at the given sequence number and validates its content.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> at which the event is expected.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the event belongs to.</param>
    /// <param name="validator">An action that validates the specific event instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when the event is not found, has the wrong type, or the validator throws.</exception>
    public static async Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        EventSequenceNumber sequenceNumber,
        EventSourceId eventSourceId,
        Action<TEvent> validator)
    {
        var events = await eventSequence.GetFromSequenceNumber(sequenceNumber, eventSourceId);
        AssertEventAtSequenceNumber(events, sequenceNumber, validator);
    }

    /// <summary>
    /// Asserts that a specific event type was appended for the given event source at the given sequence number and matches the predicate.
    /// </summary>
    /// <typeparam name="TEvent">The expected event type.</typeparam>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to assert on.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> at which the event is expected.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the event belongs to.</param>
    /// <param name="predicate">A function that returns true if the event matches expectations.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous assertion.</returns>
    /// <exception cref="EventSequenceAssertionException">Thrown when the event is not found, has the wrong type, or the predicate returns false.</exception>
    public static async Task ShouldHaveAppendedEvent<TEvent>(
        this IEventSequence eventSequence,
        EventSequenceNumber sequenceNumber,
        EventSourceId eventSourceId,
        Func<TEvent, bool> predicate)
    {
        var events = await eventSequence.GetFromSequenceNumber(sequenceNumber, eventSourceId);
        AssertEventAtSequenceNumber(events, sequenceNumber, predicate);
    }

    static void AssertAnyEventInList<TEvent>(
        IEnumerable<AppendedEvent> events,
        Action<TEvent> validator)
    {
        var envelope = events.FirstOrDefault(e => e.Content is TEvent)
            ?? throw new EventSequenceAssertionException(
                $"Expected at least one event of type '{typeof(TEvent).Name}', but none was found.");

        validator((TEvent)envelope.Content);
    }

    static void AssertAnyEventInList<TEvent>(
        IEnumerable<AppendedEvent> events,
        Func<TEvent, bool> predicate)
    {
        var matchingEvents = events.Where(e => e.Content is TEvent).ToList();

        if (matchingEvents.Count == 0)
        {
            throw new EventSequenceAssertionException(
                $"Expected at least one event of type '{typeof(TEvent).Name}', but none was found.");
        }

        if (!matchingEvents.Exists(e => predicate((TEvent)e.Content)))
        {
            throw new EventSequenceAssertionException(
                $"Expected at least one event of type '{typeof(TEvent).Name}' matching the predicate, but none matched.");
        }
    }

    static void AssertEventAtSequenceNumber<TEvent>(
        IEnumerable<AppendedEvent> events,
        EventSequenceNumber sequenceNumber,
        Action<TEvent> validator)
    {
        var envelope = events.FirstOrDefault(e => e.Context.SequenceNumber == sequenceNumber)
            ?? throw new EventSequenceAssertionException(
                $"Expected event of type '{typeof(TEvent).Name}' at sequence number {sequenceNumber}, but no event was found.");

        if (envelope.Content is not TEvent typedEvent)
        {
            throw new EventSequenceAssertionException(
                $"Expected event of type '{typeof(TEvent).Name}' at sequence number {sequenceNumber}, but found '{envelope.Content?.GetType().Name}'.");
        }

        validator(typedEvent);
    }

    static void AssertEventAtSequenceNumber<TEvent>(
        IEnumerable<AppendedEvent> events,
        EventSequenceNumber sequenceNumber,
        Func<TEvent, bool> predicate)
    {
        var envelope = events.FirstOrDefault(e => e.Context.SequenceNumber == sequenceNumber)
            ?? throw new EventSequenceAssertionException(
                $"Expected event of type '{typeof(TEvent).Name}' at sequence number {sequenceNumber}, but no event was found.");

        if (envelope.Content is not TEvent typedEvent)
        {
            throw new EventSequenceAssertionException(
                $"Expected event of type '{typeof(TEvent).Name}' at sequence number {sequenceNumber}, but found '{envelope.Content?.GetType().Name}'.");
        }

        if (!predicate(typedEvent))
        {
            throw new EventSequenceAssertionException(
                $"Event of type '{typeof(TEvent).Name}' at sequence number {sequenceNumber} did not match the predicate.");
        }
    }
}
