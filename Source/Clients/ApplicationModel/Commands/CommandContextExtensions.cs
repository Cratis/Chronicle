// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Extensions for the command context.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    /// Gets the event source id from the command context values.
    /// </summary>
    /// <param name="commandContext">The command context to get the event source id from.</param>
    /// <returns>The event source id.</returns>
    /// <exception cref="MissingEventSourceIdInCommandContext">Thrown when the event source id is missing in the command context.</exception>
    public static EventSourceId GetEventSourceId(this CommandContext commandContext)
    {
        if (commandContext.Values.TryGetValue(WellKnownCommandContextKeys.EventSourceId, out var value) && value is EventSourceId eventSourceId)
        {
            return eventSourceId;
        }

        throw new MissingEventSourceIdInCommandContext(commandContext.Command.GetType());
    }
}
