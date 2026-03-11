// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Auth;

/// <summary>
/// Settings for the auth login command.
/// </summary>
public class LoginSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the username to log in with.
    /// </summary>
    [CommandArgument(0, "<USERNAME>")]
    [Description("The username to log in with")]
    public string Username { get; set; } = string.Empty;
}
