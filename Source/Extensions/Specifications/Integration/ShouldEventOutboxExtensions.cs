// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Specifications;

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
    public static void ShouldAppendEvents(this IHaveEventOutbox eventOutbox, params object[] events) => eventOutbox.AppendedEventsToOutbox.Select(_ => _.ActualEvent).ShouldContain(events);

    /// <summary>
    /// Assert that a set events are the only ones being appended.
    /// </summary>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    /// <param name="events">Events to verify.</param>
    public static void ShouldOnlyAppendEvents(this IHaveEventOutbox eventOutbox, params object[] events) => eventOutbox.AppendedEventsToOutbox.Select(_ => _.ActualEvent).ShouldContainOnly(events);

    /// <summary>
    /// Assert that there has not been appended any events.
    /// </summary>
    /// <param name="eventOutbox">The <see cref="IHaveEventOutbox"/> the assertion is for.</param>
    public static void ShouldNotAppendEvents(this IHaveEventOutbox eventOutbox) => eventOutbox.AppendedEventsToOutbox.ShouldBeEmpty();
}
