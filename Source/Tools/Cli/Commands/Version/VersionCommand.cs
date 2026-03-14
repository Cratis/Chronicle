// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Host;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Version;

/// <summary>
/// Command that displays CLI version, server version, and contracts compatibility.
/// Does not require a running server — gracefully shows CLI-only info when unavailable.
/// </summary>
public class VersionCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var cliVersion = GetCliVersion();
        var cliContractsVersion = GetCliContractsVersion();

        // Try to connect to the server — swallow all failures silently.
        ServerVersionInfo? serverInfo = null;

        try
        {
            var connectionString = new ChronicleConnectionString(settings.ResolveConnectionString());
            var managementPort = settings.ResolveManagementPort();
            using var client = CliServiceClient.Create(connectionString, managementPort);
            serverInfo = await client.Services.Server.GetVersionInfo();
        }
        catch
        {
            // Server unavailable, misconfigured, or doesn't support GetVersionInfo — all fine.
        }

        // Check NuGet for newer versions — both fire in parallel and never block on failure.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var cliUpdateTask = UpdateChecker.CheckForUpdate(UpdateChecker.CliPackageId, cliVersion, cts.Token);
        var serverUpdateTask = serverInfo is not null
            ? UpdateChecker.CheckForUpdate(UpdateChecker.ServerPackageId, serverInfo.Version, cts.Token)
            : Task.FromResult<string?>(null);

        string? latestCli = null;
        string? latestServer = null;

        try
        {
            latestCli = await cliUpdateTask;
        }
        catch
        {
            // Non-critical.
        }

        try
        {
            latestServer = await serverUpdateTask;
        }
        catch
        {
            // Non-critical.
        }

        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
        {
            var result = new
            {
                Cli = new
                {
                    Version = cliVersion,
                    ContractsVersion = cliContractsVersion,
                    LatestVersion = latestCli
                },
                Server = serverInfo is not null
                    ? new
                    {
                        serverInfo.Version,
                        serverInfo.ContractsVersion,
                        serverInfo.CommitSha,
                        LatestVersion = latestServer
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

        if (latestCli is not null)
        {
            AnsiConsole.MarkupLine($"[yellow]CLI update available:[/]     {latestCli.EscapeMarkup()} (run 'cratis update' to upgrade)");
        }

        if (serverInfo is not null)
        {
            AnsiConsole.MarkupLine($"[bold]Server version:[/]           {serverInfo.Version.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Server contracts version:[/] {serverInfo.ContractsVersion.EscapeMarkup()}");

            if (!string.IsNullOrEmpty(serverInfo.CommitSha))
            {
                AnsiConsole.MarkupLine($"[bold]Server commit:[/]            {serverInfo.CommitSha.EscapeMarkup()}");
            }

            if (latestServer is not null)
            {
                AnsiConsole.MarkupLine($"[yellow]Server update available:[/]  {latestServer.EscapeMarkup()}");
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
            AnsiConsole.MarkupLine("[dim]Server:[/]                  [dim]unavailable[/]");
        }

        return ExitCodes.Success;
    }

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
