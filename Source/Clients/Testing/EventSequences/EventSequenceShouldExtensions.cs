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
        AssertEventInList(events, sequenceNumber, validator);
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
        AssertEventInList(events, sequenceNumber, validator);
    }

    static void AssertEventInList<TEvent>(
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
}
