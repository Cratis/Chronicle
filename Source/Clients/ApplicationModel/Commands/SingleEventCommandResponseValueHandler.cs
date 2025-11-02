// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

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
        var concurrencyScope = BuildConcurrencyScope(commandContext);
        await eventLog.Append(
            eventSourceId,
            value,
            commandContext.GetEventStreamType(),
            commandContext.GetEventStreamId(),
            commandContext.GetEventSourceType(),
            correlationId: default,
            concurrencyScope: concurrencyScope);
        return CommandResult.Success(commandContext.CorrelationId);
    }

    static ConcurrencyScope? BuildConcurrencyScope(CommandContext commandContext)
    {
        var commandType = commandContext.Command.GetType();

        var eventStreamIdAttribute = commandType.GetCustomAttributes(typeof(EventStreamIdAttribute), false).FirstOrDefault() as EventStreamIdAttribute;
        var eventStreamTypeAttribute = commandType.GetCustomAttributes(typeof(EventStreamTypeAttribute), false).FirstOrDefault() as EventStreamTypeAttribute;
        var eventSourceTypeAttribute = commandType.GetCustomAttributes(typeof(EventSourceTypeAttribute), false).FirstOrDefault() as EventSourceTypeAttribute;

        var hasAnyConcurrencyMetadata =
            (eventStreamIdAttribute?.Concurrency ?? false) ||
            (eventStreamTypeAttribute?.Concurrency ?? false) ||
            (eventSourceTypeAttribute?.Concurrency ?? false);

        if (!hasAnyConcurrencyMetadata)
        {
            return null;
        }

        var eventStreamId = (eventStreamIdAttribute?.Concurrency ?? false) ? commandContext.GetEventStreamId() : null;
        var eventStreamType = (eventStreamTypeAttribute?.Concurrency ?? false) ? commandContext.GetEventStreamType() : null;
        var eventSourceType = (eventSourceTypeAttribute?.Concurrency ?? false) ? commandContext.GetEventSourceType() : null;

        return new ConcurrencyScope(
            EventSequenceNumber.Unavailable,
            EventSourceId: null,
            EventStreamType: eventStreamType,
            EventStreamId: eventStreamId,
            EventSourceType: eventSourceType,
            EventTypes: null);
    }
}
