// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Users;

/// <summary>
/// Settings for the add user command.
/// </summary>
public class AddUserSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [CommandArgument(0, "<USERNAME>")]
    [Description("The username for the new user")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [CommandArgument(1, "<EMAIL>")]
    [Description("The email address for the new user")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [CommandArgument(2, "<PASSWORD>")]
    [Description("The initial password for the new user")]
    public string Password { get; set; } = string.Empty;
}
