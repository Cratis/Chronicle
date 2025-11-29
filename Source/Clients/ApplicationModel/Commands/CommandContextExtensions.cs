// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Extensions for the command context.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    /// Checks whether the command context has an event source id.
    /// </summary>
    /// <param name="commandContext">The command context to check.</param>
    /// <returns>True if the command context has an event source id, false otherwise.</returns>
    public static bool HasEventSourceId(this CommandContext commandContext) =>
        commandContext.Response is EventSourceId ||
        (commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceId, out var value) && value is EventSourceId);

    /// <summary>
    /// Gets the event source id from the command context values.
    /// </summary>
    /// <param name="commandContext">The command context to get the event source id from.</param>
    /// <returns>The event source id.</returns>
    /// <exception cref="MissingEventSourceIdInCommandContext">Thrown when the event source id is missing in the command context.</exception>
    public static EventSourceId GetEventSourceId(this CommandContext commandContext)
    {
        if (commandContext.Response is EventSourceId eventSourceIdResponse)
        {
            return eventSourceIdResponse;
        }

        if (commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceId, out var value) && value is EventSourceId eventSourceId)
        {
            return eventSourceId;
        }

        throw new MissingEventSourceIdInCommandContext(commandContext.Command.GetType());
    }

    /// <summary>
    /// Gets the event source type from the command context values, if present.
    /// </summary>
    /// <param name="commandContext">The command context to get the event source type from.</param>
    /// <returns>The event source type, or null if not present.</returns>
    public static EventSourceType? GetEventSourceType(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceType, out var value) && value is EventSourceType eventSourceType
            ? eventSourceType
            : null;

    /// <summary>
    /// Gets the event stream type from the command context values, if present.
    /// </summary>
    /// <param name="commandContext">The command context to get the event stream type from.</param>
    /// <returns>The event stream type, or null if not present.</returns>
    public static EventStreamType? GetEventStreamType(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventStreamType, out var value) && value is EventStreamType eventStreamType
            ? eventStreamType
            : null;

    /// <summary>
    /// Gets the event stream id from the command context values, if present.
    /// </summary>
    /// <param name="commandContext">The command context to get the event stream id from.</param>
    /// <returns>The event stream id, or null if not present.</returns>
    public static EventStreamId? GetEventStreamId(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventStreamId, out var value) && value is EventStreamId eventStreamId
            ? eventStreamId
            : null;
}
