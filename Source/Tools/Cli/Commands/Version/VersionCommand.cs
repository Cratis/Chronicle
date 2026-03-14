// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Host;
using Spectre.Console;

namespace Cratis.Chronicle.Cli.Commands.Version;

/// <summary>
/// Command that displays CLI version, server version, and contracts compatibility.
/// </summary>
public class VersionCommand : ChronicleCommand<GlobalSettings>
{
    /// <summary>
    /// Gets the CLI assembly version.
    /// </summary>
    /// <returns>The version string.</returns>
    internal static string GetCliVersion()
    {
        var assembly = typeof(CliApp).Assembly;

        return GetVersionFromAssembly(assembly);
    }

    /// <summary>
    /// Gets the contracts assembly version used by the CLI.
    /// </summary>
    /// <returns>The contracts version string.</returns>
    internal static string GetCliContractsVersion()
    {
        var assembly = typeof(IServices).Assembly;

        return GetVersionFromAssembly(assembly);
    }

    /// <summary>
    /// Determines whether CLI and server contracts are compatible based on major version.
    /// </summary>
    /// <param name="cliContracts">The CLI contracts version.</param>
    /// <param name="serverContracts">The server contracts version.</param>
    /// <returns>True if major versions match.</returns>
    internal static bool AreContractsCompatible(string cliContracts, string serverContracts)
    {
        if (!System.Version.TryParse(ExtractNumericVersion(cliContracts), out var cliVer) ||
            !System.Version.TryParse(ExtractNumericVersion(serverContracts), out var serverVer))
        {
            return cliContracts == serverContracts;
        }

        return cliVer.Major == serverVer.Major;
    }

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, GlobalSettings settings, string format)
    {
        var cliVersion = GetCliVersion();
        var cliContractsVersion = GetCliContractsVersion();

        ServerVersionInfo? serverInfo = null;

        try
        {
            serverInfo = await services.Server.GetVersionInfo();
        }
        catch
        {
            // Server may not support GetVersionInfo (older version) or may be unreachable.
        }

        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
        {
            var result = new
            {
                Cli = new
                {
                    Version = cliVersion,
                    ContractsVersion = cliContractsVersion
                },
                Server = serverInfo is not null
                    ? new
                    {
                        serverInfo.Version,
                        serverInfo.ContractsVersion,
                        serverInfo.CommitSha
                    }
                    : null,
                Compatible = serverInfo is null || AreContractsCompatible(cliContractsVersion, serverInfo.ContractsVersion),
                ServerSupportsVersionInfo = serverInfo is not null
            };

            OutputFormatter.WriteObject(format, result);
            return ExitCodes.Success;
        }

        AnsiConsole.MarkupLine($"[bold]CLI version:[/]              {cliVersion.EscapeMarkup()}");
        AnsiConsole.MarkupLine($"[bold]CLI contracts version:[/]    {cliContractsVersion.EscapeMarkup()}");

        if (serverInfo is not null)
        {
            AnsiConsole.MarkupLine($"[bold]Server version:[/]           {serverInfo.Version.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Server contracts version:[/] {serverInfo.ContractsVersion.EscapeMarkup()}");

            if (!string.IsNullOrEmpty(serverInfo.CommitSha))
            {
                AnsiConsole.MarkupLine($"[bold]Server commit:[/]            {serverInfo.CommitSha.EscapeMarkup()}");
            }

            if (AreContractsCompatible(cliContractsVersion, serverInfo.ContractsVersion))
            {
                AnsiConsole.MarkupLine("[green]Compatible[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Incompatible:[/] CLI contracts {cliContractsVersion.EscapeMarkup()} vs server contracts {serverInfo.ContractsVersion.EscapeMarkup()}");
                AnsiConsole.MarkupLine("[yellow]Suggestion:[/] Update the CLI with: dotnet tool update -g Cratis.Chronicle.Cli");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Compatible[/] (server does not support version info — update the server for full compatibility checks)");
        }

        return ExitCodes.Success;
    }

    static string GetVersionFromAssembly(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (informational is not null)
        {
            var version = informational.InformationalVersion;
            var plusIndex = version.IndexOf('+');

            return plusIndex > 0 ? version[..plusIndex] : version;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    static string ExtractNumericVersion(string version)
    {
        var dashIndex = version.IndexOf('-');

        return dashIndex > 0 ? version[..dashIndex] : version;
    }
}
