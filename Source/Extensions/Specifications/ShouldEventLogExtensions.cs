// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Holds extension methods for fluent "Should*" assertions related to <see cref="IHaveEventLog"/>.
/// </summary>
public static class ShouldEventLogExtensions
{
    /// <summary>
    /// Assert that a set events are appended.
    /// </summary>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    /// <param name="events">Events to verify.</param>
    public static void ShouldAppendEvents(this IHaveEventLog eventLog, params object[] events) => eventLog.AppendedEvents.ShouldContainEvents(events);

    /// <summary>
    /// Assert that a set events are the only ones being appended.
    /// </summary>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    /// <param name="events">Events to verify.</param>
    public static void ShouldOnlyAppendEvents(this IHaveEventLog eventLog, params object[] events) => eventLog.AppendedEvents.ShouldOnlyContainEvents(events);

    /// <summary>
    /// Assert that there has not been appended any events.
    /// </summary>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    public static void ShouldNotAppendEvents(this IHaveEventLog eventLog) => eventLog.AppendedEvents.ShouldNotContainAnyEvents();

    /// <summary>
    /// Assert that a specific appended event was appended that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    /// <param name="times">How many matches are expected.</param>
    public static void ShouldAppendEvent<T>(
        this IHaveEventLog eventLog,
        Func<T, AppendedEventForSpecifications, bool> predicate,
        Times? times = default)
        where T : class =>
        eventLog.AppendedEvents.ShouldContainEvent(predicate, times);

    /// <summary>
    /// Assert that a specific appended event was not appended that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    public static void ShouldNotAppendEvent<T>(
        this IHaveEventLog eventLog,
        Func<T, AppendedEventForSpecifications, bool> predicate)
        where T : class =>
        eventLog.AppendedEvents.ShouldNotContainEvent(predicate);

    /// <summary>
    /// Assert that a specific event was appended that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    /// <param name="times">How many matches are expected.</param>
    public static void ShouldAppendEvent<T>(
        this IHaveEventLog eventLog,
        Func<T, bool> predicate,
        Times? times = default)
        where T : class =>
        eventLog.AppendedEvents.ShouldContainEvent(predicate, times);

    /// <summary>
    /// Assert that a specific event was not appended that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    public static void ShouldNotAppendEvent<T>(
        this IHaveEventLog eventLog,
        Func<T, bool> predicate)
        where T : class =>
        eventLog.AppendedEvents.ShouldNotContainEvent(predicate);
}
