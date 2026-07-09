// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Server.Commands;

/// <summary>
/// Handles command return values that are an enumerable of Chronicle events, appending every event in order
/// to its resolved event sequence on behalf of the command.
/// </summary>
/// <param name="appender">The <see cref="CommandEventAppender"/> used to append events.</param>
public class EventsCommandResponseValueHandler(CommandEventAppender appender) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value)
    {
        if (value is not IEnumerable<object> events)
        {
            return false;
        }

        return events.All(CommandEventAppender.IsEvent);
    }

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        foreach (var @event in (IEnumerable<object>)value)
        {
            await appender.Append(commandContext.Command, @event);
        }

        return CommandResult.Success(commandContext.CorrelationId);
    }
}
