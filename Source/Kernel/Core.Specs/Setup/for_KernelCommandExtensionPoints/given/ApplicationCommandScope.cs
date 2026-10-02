// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

/// <summary>
/// Stands in for an application's transactional command scope - Arc.Chronicle's one is what deadlocks an in-process
/// kernel's start-up.
/// </summary>
public class ApplicationCommandScope : ICommandExecutionScope
{
    public void Begin(CommandContext context)
    {
        if (ApplicationExtensionPoints.Recorder is { } recorder)
        {
            recorder.ScopesBegun++;
        }
    }

    public Task Complete(CommandContext context, CommandResult result) => Task.CompletedTask;
}
