// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Cratis.Chronicle.Connections;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Global settings shared by all CLI commands.
/// </summary>
public class GlobalSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the Chronicle server connection string.
    /// </summary>
    [CommandOption("--server <CONNECTION_STRING>")]
    [Description("Chronicle server connection string (e.g. chronicle://localhost:35000)")]
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the output format.
    /// </summary>
    [CommandOption("-o|--output <FORMAT>")]
    [Description("Output format: json, text, plain, or json-compact (non-indented JSON)")]
    [DefaultValue(OutputFormats.Auto)]
    public string Output { get; set; } = OutputFormats.Auto;

    /// <summary>
    /// Gets or sets the management port for the HTTP API and token endpoint.
    /// </summary>
    [CommandOption("--management-port <PORT>")]
    [Description("Management port for the HTTP API and token endpoint (default: 8080)")]
    public int? ManagementPort { get; set; }

    /// <summary>
    /// Resolves the effective connection string by checking flag, environment variable, current context, then default.
    /// When the resolved connection string has no embedded credentials, client credentials from the context are composed in.
    /// </summary>
    /// <returns>The resolved connection string.</returns>
    public string ResolveConnectionString()
    {
        string connectionString;

        if (!string.IsNullOrWhiteSpace(Server))
        {
            connectionString = Server;
        }
        else
        {
            var envVar = Environment.GetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar);
            if (!string.IsNullOrWhiteSpace(envVar))
            {
                connectionString = envVar;
            }
            else
            {
                var config = CliConfiguration.Load();
                var ctx = config.GetCurrentContext();
                if (!string.IsNullOrWhiteSpace(ctx.Server))
                {
                    connectionString = ctx.Server;
                }
                else
                {
                    connectionString = $"chronicle://{ChronicleConnectionString.DevelopmentClient}:{ChronicleConnectionString.DevelopmentClientSecret}@localhost:35000/?disableTls=true";
                }
            }
        }

        return ComposeCredentials(connectionString);
    }

    /// <summary>
    /// Resolves the effective output format, using auto-detection when set to "auto".
    /// </summary>
    /// <returns>The resolved output format name.</returns>
    public string ResolveOutputFormat()
    {
        if (string.Equals(Output, OutputFormats.JsonCompact, StringComparison.OrdinalIgnoreCase))
        {
            return OutputFormats.JsonCompact;
        }

        if (!string.Equals(Output, OutputFormats.Auto, StringComparison.OrdinalIgnoreCase))
        {
            return Output.ToLowerInvariant();
        }

        var noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        if (noColor is not null)
        {
            return OutputFormats.Plain;
        }

        return Console.IsOutputRedirected ? OutputFormats.Json : OutputFormats.Text;
    }

    /// <summary>
    /// Resolves the effective management port by checking flag, environment variable, current context, then default.
    /// </summary>
    /// <returns>The resolved management port.</returns>
    public int ResolveManagementPort()
    {
        if (ManagementPort.HasValue)
        {
            return ManagementPort.Value;
        }

        var envVar = Environment.GetEnvironmentVariable(CliDefaults.ManagementPortEnvVar);
        if (!string.IsNullOrWhiteSpace(envVar) && int.TryParse(envVar, out var envPort))
        {
            return envPort;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        if (ctx.ManagementPort.HasValue)
        {
            return ctx.ManagementPort.Value;
        }

        return CliDefaults.DefaultManagementPort;
    }

    static string ComposeCredentials(string connectionString)
    {
        var parsed = new ChronicleConnectionString(connectionString);
        if (!string.IsNullOrEmpty(parsed.Username))
        {
            return connectionString;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        if (!string.IsNullOrWhiteSpace(ctx.ClientId) && !string.IsNullOrWhiteSpace(ctx.ClientSecret))
        {
            return parsed.WithCredentials(ctx.ClientId, ctx.ClientSecret).ToString();
        }

        return connectionString;
    }
}
