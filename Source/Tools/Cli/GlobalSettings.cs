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
    [Description("Output format: json, text, or plain")]
    [DefaultValue("auto")]
    public string Output { get; set; } = "auto";

    /// <summary>
    /// Resolves the effective connection string by checking flag, environment variable, config file, then default.
    /// </summary>
    /// <returns>The resolved connection string.</returns>
    public string ResolveConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(Server))
        {
            return Server;
        }

        var envVar = Environment.GetEnvironmentVariable("CHRONICLE_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envVar))
        {
            return envVar;
        }

        var config = CliConfiguration.Load();
        if (!string.IsNullOrWhiteSpace(config.DefaultServer))
        {
            return config.DefaultServer;
        }

        return $"chronicle://{ChronicleConnectionString.DevelopmentClient}:{ChronicleConnectionString.DevelopmentClientSecret}@localhost:35000";
    }

    /// <summary>
    /// Resolves the effective output format, using auto-detection when set to "auto".
    /// </summary>
    /// <returns>The resolved output format name.</returns>
    public string ResolveOutputFormat()
    {
        if (!string.Equals(Output, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return Output.ToLowerInvariant();
        }

        var noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        if (noColor is not null)
        {
            return "plain";
        }

        return Console.IsOutputRedirected ? "json" : "text";
    }
}
