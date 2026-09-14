// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Testing.for_InProcessCommandPipeline;

public interface IConsumerScope
{
    void Begin(CommandContext context);
    void Complete(CommandContext context, CommandResult result);
}
