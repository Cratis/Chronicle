// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Version;

/// <summary>
/// Settings for the <see cref="SelfUpdateCommand"/>.
/// </summary>
public class SelfUpdateSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets a specific version to install. When not set, updates to the latest.
    /// </summary>
    [CommandOption("--version <VERSION>")]
    [System.ComponentModel.Description("Specific version to install (default: latest)")]
    public string? TargetVersion { get; set; }

    /// <summary>
    /// Gets or sets the output format.
    /// </summary>
    [CommandOption("-o|--output <FORMAT>")]
    [System.ComponentModel.Description("Output format: json, text, plain, or json-compact")]
    [System.ComponentModel.DefaultValue(OutputFormats.Auto)]
    public string Output { get; set; } = OutputFormats.Auto;
}
