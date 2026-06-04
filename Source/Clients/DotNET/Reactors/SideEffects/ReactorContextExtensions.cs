// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Extension methods for <see cref="ReactorContext"/> that read append-metadata from the
/// <see cref="ReactorContext.Values"/> populated by the registered <see cref="IReactorContextValuesProvider"/> instances.
/// </summary>
public static class ReactorContextExtensions
{
    /// <summary>
    /// Gets the <see cref="EventSourceId"/> to use when appending side-effect events.
    /// </summary>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The resolved <see cref="EventSourceId"/>, or the <see cref="EventSourceId"/> of the triggering event when none was provided.</returns>
    public static EventSourceId GetEventSourceId(this ReactorContext reactorContext) =>
        reactorContext.Values.TryGetValue(WellKnownReactorContextKeys.EventSourceId, out var value) && value is EventSourceId eventSourceId
            ? eventSourceId
            : reactorContext.EventContext.EventSourceId;

    /// <summary>
    /// Gets the <see cref="EventStreamType"/> to use when appending side-effect events.
    /// </summary>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The <see cref="EventStreamType"/>, or <see langword="null"/> when none was provided.</returns>
    public static EventStreamType? GetEventStreamType(this ReactorContext reactorContext) =>
        reactorContext.Values.TryGetValue(WellKnownReactorContextKeys.EventStreamType, out var value) && value is EventStreamType eventStreamType
            ? eventStreamType
            : null;

    /// <summary>
    /// Gets the <see cref="EventStreamId"/> to use when appending side-effect events.
    /// </summary>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The resolved <see cref="EventStreamId"/>, or <see langword="null"/> when none was provided.</returns>
    public static EventStreamId? GetEventStreamId(this ReactorContext reactorContext) =>
        reactorContext.Values.TryGetValue(WellKnownReactorContextKeys.EventStreamId, out var value) && value is EventStreamId eventStreamId
            ? eventStreamId
            : null;

    /// <summary>
    /// Gets the <see cref="EventSourceType"/> to use when appending side-effect events.
    /// </summary>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The <see cref="EventSourceType"/>, or <see langword="null"/> when none was provided.</returns>
    public static EventSourceType? GetEventSourceType(this ReactorContext reactorContext) =>
        reactorContext.Values.TryGetValue(WellKnownReactorContextKeys.EventSourceType, out var value) && value is EventSourceType eventSourceType
            ? eventSourceType
            : null;

    /// <summary>
    /// Gets the <see cref="Subject"/> to use when appending side-effect events.
    /// </summary>
    /// <param name="reactorContext">The reactor context.</param>
    /// <returns>The <see cref="Subject"/>, or <see langword="null"/> when none was provided.</returns>
    public static Subject? GetSubject(this ReactorContext reactorContext) =>
        reactorContext.Values.TryGetValue(WellKnownReactorContextKeys.Subject, out var value) && value is Subject subject
            ? subject
            : null;
}
