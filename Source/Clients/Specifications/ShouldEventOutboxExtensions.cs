// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Holds extension methods for fluent "Should*" assertions related to <see cref="IHaveEventOutbox"/>.
/// </summary>
public static class ShouldEventOutboxExtensions
{
    /// <summary>
    /// Assert that a set events are appended.
    /// </summary>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    /// <param name="events">Events to verify.</param>
    public static void ShouldAppendEventsToOutbox(this IHaveEventOutbox eventOutbox, params object[] events) => eventOutbox.AppendedEventsToOutbox.ShouldContainEvents(events);

    /// <summary>
    /// Assert that a set events are the only ones being appended.
    /// </summary>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    /// <param name="events">Events to verify.</param>
    public static void ShouldOnlyAppendEventsToOutbox(this IHaveEventOutbox eventOutbox, params object[] events) => eventOutbox.AppendedEventsToOutbox.ShouldOnlyContainEvents(events);

    /// <summary>
    /// Assert that there has not been appended any events.
    /// </summary>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    public static void ShouldNotAppendEventsToOutbox(this IHaveEventOutbox eventOutbox) => eventOutbox.AppendedEventsToOutbox.ShouldNotContainAnyEvents();

    /// <summary>
    /// Assert that a specific appended event was appended that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    /// <param name="times">How many matches are expected.</param>
    public static void ShouldAppendEventToOutbox<T>(
        this IHaveEventOutbox eventOutbox,
        Func<T, AppendedEventForSpecifications, bool> predicate,
        Times? times = default)
        where T : class =>
        eventOutbox.AppendedEventsToOutbox.ShouldContainEvent(predicate, times);

    /// <summary>
    /// Assert that a specific appended event was not appended that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    public static void ShouldNotAppendEventToOutbox<T>(
        this IHaveEventOutbox eventOutbox,
        Func<T, AppendedEventForSpecifications, bool> predicate)
        where T : class =>
        eventOutbox.AppendedEventsToOutbox.ShouldNotContainEvent(predicate);

    /// <summary>
    /// Assert that a specific event was appended that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    /// <param name="times">How many matches are expected.</param>
    public static void ShouldAppendEventToOutbox<T>(
        this IHaveEventOutbox eventOutbox,
        Func<T, bool> predicate,
        Times? times = default)
        where T : class =>
        eventOutbox.AppendedEventsToOutbox.ShouldContainEvent(predicate, times);

    /// <summary>
    /// Assert that a specific event was not appended that matches the criteria of the callback.
    /// </summary>
    /// <typeparam name="T">Type of event.</typeparam>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    /// <param name="predicate">Predicate callback.</param>
    public static void ShouldNotAppendEventToOutbox<T>(
        this IHaveEventOutbox eventOutbox,
        Func<T, bool> predicate)
        where T : class =>
        eventOutbox.AppendedEventsToOutbox.ShouldNotContainEvent(predicate);
}
