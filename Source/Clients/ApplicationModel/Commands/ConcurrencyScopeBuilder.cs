// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Builder for creating concurrency scopes based on command context and metadata attributes.
/// </summary>
public static class ConcurrencyScopeBuilder
{
    /// <summary>
    /// Builds a concurrency scope based on metadata attributes that have Concurrency flag set to true.
    /// </summary>
    /// <param name="commandContext">The command context containing the command.</param>
    /// <returns>A concurrency scope if any metadata has concurrency enabled, otherwise null.</returns>
    public static ConcurrencyScope? BuildFromCommandContext(CommandContext commandContext)
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

        // EventSourceId is intentionally null as concurrency scoping is based on metadata (EventStreamId, EventStreamType, EventSourceType)
        // rather than the event source itself.
        return new ConcurrencyScope(
            EventSequenceNumber.Unavailable,
            EventSourceId: null,
            EventStreamType: eventStreamType,
            EventStreamId: eventStreamId,
            EventSourceType: eventSourceType,
            EventTypes: null);
    }
}
