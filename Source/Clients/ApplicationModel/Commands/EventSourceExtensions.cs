// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Applications.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Provides extension methods for working with event sources in command contexts.
/// </summary>
public static class EventSourceExtensions
{
    /// <summary>
    /// Determines whether the command context has an event source ID associated with the command or value.
    /// </summary>
    /// <param name="commandContext">The command context.</param>
    /// <param name="value">The value to check for an event source ID.</param>
    /// <returns>True if the command context has an event source ID; otherwise, false.</returns>
    public static bool HasEventSourceId(this CommandContext commandContext, object value) =>
        commandContext.Command.GetType().GetProperties().Any(p => p.PropertyType.IsAssignableTo(typeof(EventSourceId))) ||
        (value is ITuple tuple && tuple.Length > 0 && tuple[0] is EventSourceId);

    /// <summary>
    /// Gets the event source ID associated with the command or value in the command context.
    /// </summary>
    /// <param name="commandContext">The command context.</param>
    /// <param name="value">The value to get the event source ID from.</param>
    /// <returns>The event source ID.</returns>
    public static EventSourceId GetEventSourceId(this CommandContext commandContext, object value)
    {
        var eventSourceId = EventSourceId.Unspecified;

        if (value is ITuple tuple && tuple.Length > 0 && tuple[0] is EventSourceId id)
        {
            eventSourceId = id;
        }
        else
        {
            var property = commandContext.Command.GetType().GetProperties()
                .FirstOrDefault(p => p.PropertyType.IsAssignableTo(typeof(EventSourceId)));

            if (property is not null)
            {
                eventSourceId = (EventSourceId)property.GetValue(commandContext.Command)!;
            }
        }

        return eventSourceId;
    }
}
