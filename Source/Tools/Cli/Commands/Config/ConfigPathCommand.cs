// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Config;

/// <summary>
/// Prints the configuration file path.
/// </summary>
public class ConfigPathCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings)
    {
        Console.WriteLine(CliConfiguration.GetConfigPath());
        return Task.FromResult(ExitCodes.Success);
    }
}
