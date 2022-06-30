// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Specifications;
using Moq;
using Xunit;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Holds extension methods for fluent "Should*" assertions related to enumerable of <see cref="AppendedEventForSpecifications"/>.
/// </summary>
public static class ShouldAppendedEventsExtensions
{
    /// <summary>
    /// Assert that a set events are appended.
    /// </summary>
    /// <param name="appendedEvents">The appended events the assertion is for.</param>
    /// <param name="events">Events to verify.</param>
    public static void ShouldContainEvents(this IEnumerable<AppendedEventForSpecifications> appendedEvents, params object[] events) => appendedEvents.Select(_ => _.ActualEvent).ShouldContain(events);

    /// <summary>
    /// Assert that a set events are the only ones being appended.
    /// </summary>
    /// <param name="appendedEvents">The appended events the assertion is for.</param>
    /// <param name="events">Events to verify.</param>
    public static void ShouldOnlyContainEvents(this IEnumerable<AppendedEventForSpecifications> appendedEvents, params object[] events) => appendedEvents.Select(_ => _.ActualEvent).ShouldContainOnly(events);

    /// <summary>
    /// Assert that there has not been appended any events.
    /// </summary>
    /// <param name="appendedEvents">The appended events the assertion is for.</param>
    public static void ShouldNotContainAnyEvents(this IEnumerable<AppendedEventForSpecifications> appendedEvents) => appendedEvents.ShouldBeEmpty();

    /// <summary>
    /// Assert that a specific appended event that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="appendedEvents">The appended events the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    /// <param name="times">How many matches are expected.</param>
    public static void ShouldContainEvent<T>(
        this IEnumerable<AppendedEventForSpecifications> appendedEvents,
        Func<T, AppendedEventForSpecifications, bool> predicate,
        Times? times = default)
        where T : class
    {
        times ??= Times.AtLeastOnce();
        var matches = appendedEvents.Count(_ => _.ActualEvent is T && predicate((_.ActualEvent as T)!, _));
        Assert.True(times?.Validate(matches), $"Expected event type {typeof(T).FullName}, {times}");
    }

    /// <summary>
    /// Assert that a specific appended event is not present that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="appendedEvents">The appended events the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    public static void ShouldNotContainEvent<T>(
        this IEnumerable<AppendedEventForSpecifications> appendedEvents,
        Func<T, AppendedEventForSpecifications, bool> predicate)
        where T : class =>
        ShouldContainEvent(appendedEvents, predicate, Times.Never());

    /// <summary>
    /// Assert that a specific event that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="appendedEvents">The appended events the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    /// <param name="times">How many matches are expected.</param>
    public static void ShouldContainEvent<T>(
        this IEnumerable<AppendedEventForSpecifications> appendedEvents,
        Func<T, bool> predicate,
        Times? times = default)
        where T : class
    {
        times ??= Times.AtLeastOnce();
        var matches = appendedEvents.Count(_ => _.ActualEvent is T && predicate((_.ActualEvent as T)!));
        Assert.True(times?.Validate(matches), $"Expected event type {typeof(T).FullName}, {times}");
    }

    /// <summary>
    /// Assert that a specific event is not present that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="appendedEvents">The appended events the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    public static void ShouldNotContainEvent<T>(this IEnumerable<AppendedEventForSpecifications> appendedEvents, Func<T, bool> predicate)
        where T : class =>
        ShouldContainEvent(appendedEvents, predicate, Times.Never());
}
