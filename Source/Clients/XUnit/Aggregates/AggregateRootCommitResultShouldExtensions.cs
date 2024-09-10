// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Xunit.Sdk;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Holds extension methods for fluent "Should*" assertions related to <see cref="AggregateRootCommitResult"/>.
/// </summary>
public static class AggregateRootCommitResultShouldExtensions
{
    /// <summary>
    /// Asserts that the <see cref="AggregateRootCommitResult"/> is successful.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert.</param>
    public static void ShouldBeSuccessful(this AggregateRootCommitResult result) => Assert.True(result.IsSuccess);

    /// <summary>
    /// Asserts that the <see cref="AggregateRootCommitResult"/> is successful.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert.</param>
    public static void ShouldBeFailed(this AggregateRootCommitResult result) => Assert.False(result.IsSuccess);

    /// <summary>
    /// Asserts that the <see cref="AggregateRootCommitResult"/> contains no events.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert.</param>
    public static void ShouldNotContainAnyEvents(this AggregateRootCommitResult result)
    {
        var count = result.Events.Count();
        Assert.True(count == 0, $"Expected no events, but found {count}");
    }

    /// <summary>
    /// Asserts that the <see cref="AggregateRootCommitResult"/> contains a specific set of events, not necessarily in the same order and not exclusively.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert.</param>
    /// <param name="events">Collection of events to check for.</param>
    public static void ShouldContainEvents(this AggregateRootCommitResult result, params object[] events)
    {
        var containsAll = result.Events.All(e => events.Contains(e));
        Assert.True(containsAll, "Expected all events to be present, but some were missing");
    }

    /// <summary>
    /// Asserts that the <see cref="AggregateRootCommitResult"/> contains a specific set of events based on the predicates, with strict ordering.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert.</param>
    /// <param name="predicates">The assertion predicates to match in order.</param>
    /// <exception cref="XunitException">Throws when number of events does not match number of predicates.</exception>
    public static void ShouldContainEvents(this AggregateRootCommitResult result, params Action<object>[] predicates)
    {
        var events = result.Events.ToArray();
        if (events.Length != predicates.Length)
        {
            throw new XunitException("Number of events does not match number of predicates");
        }

        for (var i = 0; i < events.Length; i++)
        {
            predicates[i](events[i]);
        }
    }

    /// <summary>
    /// Asserts that the <see cref="AggregateRootCommitResult"/> contains events in loose ordering based on the given predicates.
    /// </summary>
    /// <remarks>
    /// This method is useful if you want to ensure some consistency on event ordering, but you cannot 100% guarantee the ordering of all events.
    /// </remarks>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert.</param>
    /// <param name="predicates">The assertion predicates to match in order.</param>
    /// <exception cref="XunitException">Throws when the order of the matches is off.</exception>
    public static void ShouldContainEventsInOrder(this AggregateRootCommitResult result, params Func<object, bool>[] predicates)
    {
        var events = result.Events.ToList();
        var lastFoundIndex = 0;
        for (var i = 0; i < predicates.Length; i++)
        {
            var predicate = predicates[i];
            var foundIndex = events.FindIndex(lastFoundIndex, _ => predicate(_));
            if (foundIndex == -1)
            {
                throw new XunitException($"Could not find an event matching predicate {i + 1} after the event matching the last predicate");
            }

            lastFoundIndex = foundIndex + 1;
        }
    }

    /// <summary>
    /// Assert that the <see cref="AggregateRootCommitResult"/> contains a specific event.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert on.</param>
    /// <param name="times">Optional number of times the event should be present. Defaults to 1.</param>
    /// <typeparam name="TEvent">Type of event to assert.</typeparam>
    public static void ShouldContainEvent<TEvent>(this AggregateRootCommitResult result, int times = 1)
        where TEvent : class
    {
        var count = result.Events.Count(e => e is TEvent);
        Assert.True(times == count, $"Expected {times} events of type {typeof(TEvent).FullName}, but found {count}");
    }

    /// <summary>
    /// Assert that the <see cref="AggregateRootCommitResult"/> contains a specific event.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert on.</param>
    /// <param name="predicate">Predicate to match the event.</param>
    /// <param name="times">Optional number of times the event should be present. Defaults to 1.</param>
    /// <typeparam name="TEvent">Type of event to assert.</typeparam>
    public static void ShouldContainEvent<TEvent>(
        this AggregateRootCommitResult result,
        Func<TEvent, bool> predicate,
        int times = 1)
        where TEvent : class
    {
        var matches = result.Events.Count(_ => _ is TEvent && predicate((_ as TEvent)!));
        Assert.True(matches == times, $"Expected event type {typeof(TEvent).FullName} to match predicate {times} times, but found {matches}");
    }

    /// <summary>
    /// Assert that the <see cref="AggregateRootCommitResult"/> contains a specific event.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert on.</param>
    /// <typeparam name="TEvent">Type of event to assert.</typeparam>
    public static void ShouldNotContainEvent<TEvent>(this AggregateRootCommitResult result)
        where TEvent : class
    {
        var count = result.Events.Count(e => e is TEvent);
        Assert.True(count == 0, $"Expected no events of type {typeof(TEvent).FullName}, but found {count}");
    }

    /// <summary>
    /// Assert that the <see cref="AggregateRootCommitResult"/> contains a specific event.
    /// </summary>
    /// <param name="result"><see cref="AggregateRootCommitResult"/> to assert on.</param>
    /// <param name="predicate">Predicate to match the event.</param>
    /// <typeparam name="TEvent">Type of event to assert.</typeparam>
    public static void ShouldNotContainEvent<TEvent>(
        this AggregateRootCommitResult result,
        Func<TEvent, bool> predicate)
        where TEvent : class
    {
        var matches = result.Events.Count(_ => _ is TEvent && predicate((_ as TEvent)!));
        Assert.True(matches == 0, $"Expected no events of type {typeof(TEvent).FullName} according to predicate, but found {matches}");
    }

    /// <summary>
    /// Performs a type and match check on the given event.
    /// </summary>
    /// <param name="subject">The event to check.</param>
    /// <param name="match">The match predicate that should be true.</param>
    /// <typeparam name="TEvent">Type of the event to assert.</typeparam>
    public static void ShouldMatchEvent<TEvent>(this object subject, Func<TEvent, bool> match)
        where TEvent : class
    {
        var evt = Assert.IsType<TEvent>(subject);
        Assert.True(match(evt));
    }

    /// <summary>
    /// Performs a type check and assertion on the given event.
    /// </summary>
    /// <param name="subject">The event to check.</param>
    /// <param name="assert">The assertion predicate.</param>
    /// <typeparam name="TEvent">Type of the event to assert.</typeparam>
    public static void AssertOnEvent<TEvent>(this object subject, Action<TEvent> assert)
        where TEvent : class
    {
        var evt = Assert.IsType<TEvent>(subject);
        assert(evt);
    }
}
