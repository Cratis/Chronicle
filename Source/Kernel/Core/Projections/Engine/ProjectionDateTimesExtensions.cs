// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Extension methods for preparing events for an <see cref="IProjection"/> with <see cref="ProjectionDateTimes"/>.
/// </summary>
public static class ProjectionDateTimesExtensions
{
    /// <summary>
    /// Gets an event with its <c language="json">date-time</c> values held the way the projection computes from them.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> the event enters.</param>
    /// <param name="event">The event as read.</param>
    /// <returns>The event to project; the same instance when nothing changes.</returns>
    public static AppendedEvent ForProjection(this IProjection projection, AppendedEvent @event) =>
        projection is Projection engineProjection ? engineProjection.DateTimes.ForProjection(@event) : @event;

    /// <summary>
    /// Gets events with their <c language="json">date-time</c> values held the way the projection computes from them.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> the events enter.</param>
    /// <param name="events">The events as read.</param>
    /// <returns>The events to project.</returns>
    public static AppendedEvent[] ForProjection(this IProjection projection, IEnumerable<AppendedEvent> events) =>
        [.. events.Select(projection.ForProjection)];
}
