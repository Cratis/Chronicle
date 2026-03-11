// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Config;

/// <summary>
/// Settings for the set config command.
/// </summary>
public class SetConfigSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    [CommandArgument(0, "<KEY>")]
    [Description("Configuration key (server, event-store, namespace)")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    [CommandArgument(1, "<VALUE>")]
    [Description("The value to set")]
    public string Value { get; set; } = string.Empty;
}
