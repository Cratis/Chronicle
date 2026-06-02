// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Server.Commands;

/// <summary>
/// Handles command return values that are a single Chronicle event, appending the event to the resolved
/// event sequence on behalf of the command.
/// </summary>
/// <param name="appender">The <see cref="CommandEventAppender"/> used to append the event.</param>
public class SingleEventCommandResponseValueHandler(CommandEventAppender appender) : ICommandResponseValueHandler
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) => CommandEventAppender.IsEvent(value);

    /// <inheritdoc/>
    public async Task<CommandResult> Handle(CommandContext commandContext, object value)
    {
        await appender.Append(commandContext.Command, value);
        return CommandResult.Success(commandContext.CorrelationId);
    }
}
