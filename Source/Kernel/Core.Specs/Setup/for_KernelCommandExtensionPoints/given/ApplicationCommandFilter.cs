// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

public class ApplicationCommandFilter : ICommandFilter
{
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        if (ApplicationExtensionPoints.Recorder is { } recorder)
        {
            recorder.FiltersRun++;
        }

        return Task.FromResult(CommandResult.Success(context.CorrelationId));
    }
}
