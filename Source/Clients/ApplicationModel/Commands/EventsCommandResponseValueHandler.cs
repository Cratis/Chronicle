// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Represents a command response value handler that can handle multiple events as the response value.
/// </summary>
/// <param name="eventLog">The event log to append events to.</param>
/// <param name="eventTypes">The event types.</param>
public class EventsCommandResponseValueHandler(IEventLog eventLog, IEventTypes eventTypes) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        (value is IEnumerable<object> objects) &&
        objects.All(o => eventTypes.HasFor(o.GetType())) &&
        commandContext.HasEventSourceId();

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var eventSourceId = commandContext.GetEventSourceId();
        var events = (IEnumerable<object>)value;
        if (events.Any())
        {
            var concurrencyScope = ConcurrencyScopeBuilder.BuildFromCommandContext(commandContext);
            var result = await eventLog.AppendMany(
                eventSourceId,
                events,
                commandContext.GetEventStreamType(),
                commandContext.GetEventStreamId(),
                commandContext.GetEventSourceType(),
                correlationId: default,
                concurrencyScope: concurrencyScope);

            if (!result.IsSuccess)
            {
                return result.ToCommandResult();
            }
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
