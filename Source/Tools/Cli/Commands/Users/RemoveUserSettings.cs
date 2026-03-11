// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Users;

/// <summary>
/// Settings for the remove user command.
/// </summary>
public class RemoveUserSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the user ID to remove.
    /// </summary>
    [CommandArgument(0, "<USER_ID>")]
    [Description("The unique identifier of the user to remove")]
    public Guid UserId { get; set; }
}
