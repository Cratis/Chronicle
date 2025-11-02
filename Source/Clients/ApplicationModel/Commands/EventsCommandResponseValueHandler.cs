// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Commands;
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
        commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceId, out var id) && id is EventSourceId;

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        var eventSourceId = commandContext.GetEventSourceId();
        var events = (IEnumerable<object>)value;
        if (events.Any())
        {
            var eventSourceType = commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceType, out var estValue) && estValue is EventSourceType est ? est : null;
            var eventStreamType = commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventStreamType, out var estrValue) && estrValue is EventStreamType estr ? estr : null;
            var eventStreamId = commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventStreamId, out var esiValue) && esiValue is EventStreamId esi ? esi : null;

            await eventLog.AppendMany(eventSourceId, events, eventStreamType, eventStreamId, eventSourceType);
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
