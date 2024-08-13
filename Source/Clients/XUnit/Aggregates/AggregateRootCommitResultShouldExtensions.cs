// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

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
}
