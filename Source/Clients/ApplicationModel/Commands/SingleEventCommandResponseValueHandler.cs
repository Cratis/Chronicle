// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Represents a command response value handler that can handle a single event as the response value.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
public class SingleEventCommandResponseValueHandler(IEventLog eventLog, IEventTypes eventTypes) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is object obj &&
        eventTypes.HasFor(obj.GetType()) &&
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceId, out var id) && id is EventSourceId;

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var eventSourceId = commandContext.GetEventSourceId();
        await eventLog.Append(
            eventSourceId,
            value,
            commandContext.GetEventStreamType(),
            commandContext.GetEventStreamId(),
            commandContext.GetEventSourceType());
        return CommandResult.Success(commandContext.CorrelationId);
    }
}
