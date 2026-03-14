// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Applications;

/// <summary>
/// Settings for the add application command.
/// </summary>
public class AddApplicationSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the client identifier for the application.
    /// </summary>
    [CommandArgument(0, "<CLIENT_ID>")]
    [Description("The client identifier for the new application")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret for the application.
    /// </summary>
    [CommandArgument(1, "<CLIENT_SECRET>")]
    [Description("The client secret for the new application")]
    public string ClientSecret { get; set; } = string.Empty;
}
