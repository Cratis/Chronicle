// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Reflection;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Provides extension methods for working with event sources in command contexts.
/// </summary>
public static class EventSourceExtensions
{
    /// <summary>
    /// Determines whether the command has an event source ID associated with it.
    /// </summary>
    /// <param name="command">The command to check for an event source ID.</param>
    /// <returns>True if the command has an event source ID; otherwise, false.</returns>
    public static bool HasEventSourceId(this object command) =>
        command.GetType().GetProperties().Any(p =>
            p.PropertyType.IsAssignableTo(typeof(EventSourceId)) ||
            p.HasAttribute<KeyAttribute>()) ||
            ((command is ITuple tuple) && tuple.HasEventSourceId());

    /// <summary>
    /// Determines whether the tuple has an event source ID.
    /// </summary>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>True if the tuple has an event source ID; otherwise, false.</returns>
    public static bool HasEventSourceId(this ITuple tuple)
    {
        var values = new List<object?>();
        for (var i = 0; i < tuple.Length; i++)
        {
            values.Add(tuple[i]);
        }

        return values.Exists(v => v is EventSourceId);
    }

    /// <summary>
    /// Gets the event source ID associated with the command.
    /// </summary>
    /// <param name="command">The command to get the event source ID from.</param>
    /// <returns>The event source ID.</returns>
    public static EventSourceId GetEventSourceId(this object command)
    {
        var eventSourceId = EventSourceId.Unspecified;

        if (command is ITuple tuple)
        {
            var values = new List<object?>();
            for (var i = 0; i < tuple.Length; i++)
            {
                values.Add(tuple[i]);
            }

            var id = values.Find(v => v is EventSourceId);
            if (id is not null)
            {
                eventSourceId = (EventSourceId)id;
            }
        }
        else
        {
            var property = command.GetType().GetProperties()
                .FirstOrDefault(p => p.PropertyType.IsAssignableTo(typeof(EventSourceId)) || p.HasAttribute<KeyAttribute>());

            if (property is not null)
            {
                var value = property.GetValue(command);
                if (value is not null)
                {
                    if (value is EventSourceId esId)
                    {
                        eventSourceId = esId;
                    }
                    else
                    {
                        // Handle properties marked with [Key] attribute that are not EventSourceId type
                        eventSourceId = value.ToString()!;
                    }
                }
            }
        }

        return eventSourceId;
    }
}
