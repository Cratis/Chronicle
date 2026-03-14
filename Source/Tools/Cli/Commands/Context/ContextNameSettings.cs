// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Context;

/// <summary>
/// Settings for commands that take a context name argument.
/// </summary>
public class ContextNameSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the context name.
    /// </summary>
    [CommandArgument(0, "<NAME>")]
    [Description("Name of the context")]
    public string Name { get; set; } = string.Empty;
}
