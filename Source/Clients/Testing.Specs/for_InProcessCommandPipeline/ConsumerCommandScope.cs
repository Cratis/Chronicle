// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Testing.for_InProcessCommandPipeline;

public class ConsumerCommandScope(IConsumerScope consumerScope) : ICommandExecutionScope
{
    public void Begin(CommandContext context) => consumerScope.Begin(context);

    public Task Complete(CommandContext context, CommandResult result)
    {
        consumerScope.Complete(context, result);
        return Task.CompletedTask;
    }
}
