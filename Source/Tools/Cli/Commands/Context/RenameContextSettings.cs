// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Context;

/// <summary>
/// Settings for the context rename command.
/// </summary>
public class RenameContextSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the current name of the context.
    /// </summary>
    [CommandArgument(0, "<OLD_NAME>")]
    [Description("Current name of the context")]
    public string OldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new name for the context.
    /// </summary>
    [CommandArgument(1, "<NEW_NAME>")]
    [Description("New name for the context")]
    public string NewName { get; set; } = string.Empty;
}
