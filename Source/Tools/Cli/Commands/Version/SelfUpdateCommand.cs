// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Version;

/// <summary>
/// Updates the Cratis CLI to the latest (or a specific) version using dotnet tool update.
/// </summary>
public class SelfUpdateCommand : AsyncCommand<SelfUpdateSettings>
{
    const string PackageId = "Cratis.Chronicle.Cli";

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, SelfUpdateSettings settings, CancellationToken cancellationToken)
    {
        var format = ResolveFormat(settings.Output);
        var currentVersion = VersionCommand.GetCliVersion();

        var arguments = $"tool update -g {PackageId}";
        if (!string.IsNullOrWhiteSpace(settings.TargetVersion))
        {
            arguments += $" --version {settings.TargetVersion}";
        }

        if (format is OutputFormats.Text)
        {
            AnsiConsole.MarkupLine($"[bold]Updating Cratis CLI...[/] (current: {currentVersion.EscapeMarkup()})");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            OutputFormatter.WriteError(format, "Failed to start dotnet process", "Ensure the .NET SDK is installed and 'dotnet' is on your PATH");
            return ExitCodes.ServerError;
        }

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var errorMessage = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : stdout.Trim();
            OutputFormatter.WriteError(format, $"Update failed: {errorMessage}");
            return ExitCodes.ServerError;
        }

        var newVersion = VersionCommand.GetCliVersion();

        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
        {
            OutputFormatter.WriteObject(format, new
            {
                PreviousVersion = currentVersion,
                CurrentVersion = newVersion,
                Updated = currentVersion != newVersion
            });
        }
        else if (currentVersion != newVersion)
        {
            AnsiConsole.MarkupLine($"[green]Updated from {currentVersion.EscapeMarkup()} to {newVersion.EscapeMarkup()}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Already at the latest version ({currentVersion.EscapeMarkup()})[/]");
        }

        return ExitCodes.Success;
    }

    static string ResolveFormat(string output)
    {
        if (string.Equals(output, OutputFormats.JsonCompact, StringComparison.OrdinalIgnoreCase))
        {
            return OutputFormats.JsonCompact;
        }

        if (!string.Equals(output, OutputFormats.Auto, StringComparison.OrdinalIgnoreCase))
        {
            return output.ToLowerInvariant();
        }

        return Console.IsOutputRedirected ? OutputFormats.Json : OutputFormats.Text;
    }
}
