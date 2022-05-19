// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Specifications;

namespace Aksio.Cratis.Specifications.Integration;

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
    public static void ShouldAppendEvents(this IHaveEventLog eventLog, params object[] events) => eventLog.AppendedEvents.Select(_ => _.ActualEvent).ShouldContain(events);

    /// <summary>
    /// Assert that a set events are the only ones being appended.
    /// </summary>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    /// <param name="events">Events to verify.</param>
    public static void ShouldOnlyAppendEvents(this IHaveEventLog eventLog, params object[] events) => eventLog.AppendedEvents.Select(_ => _.ActualEvent).ShouldContainOnly(events);

    /// <summary>
    /// Assert that there has not been appended any events.
    /// </summary>
    /// <param name="eventLog">The <see cref="IHaveEventLog"/> the assertion is for.</param>
    public static void ShouldNotAppendEvents(this IHaveEventLog eventLog) => eventLog.AppendedEvents.ShouldBeEmpty();
}
