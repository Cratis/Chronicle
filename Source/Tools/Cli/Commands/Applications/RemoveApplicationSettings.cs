// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Applications;

/// <summary>
/// Settings for the remove application command.
/// </summary>
public class RemoveApplicationSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the application ID to remove.
    /// </summary>
    [CommandArgument(0, "<APP_ID>")]
    [Description("The unique identifier of the application to remove")]
    public Guid AppId { get; set; }
}
