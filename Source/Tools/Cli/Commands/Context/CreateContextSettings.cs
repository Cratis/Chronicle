// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Context;

/// <summary>
/// Settings for creating a new context.
/// </summary>
public class CreateContextSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the name of the context to create.
    /// </summary>
    [CommandArgument(0, "<NAME>")]
    [Description("Name of the context to create")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server connection string for the new context.
    /// </summary>
    [CommandOption("--server <CONNECTION_STRING>")]
    [Description("Chronicle server connection string for this context")]
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the event store name for the new context.
    /// </summary>
    [CommandOption("-e|--event-store <NAME>")]
    [Description("Default event store for this context")]
    public string? EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace for the new context.
    /// </summary>
    [CommandOption("-n|--namespace <NAME>")]
    [Description("Default namespace for this context")]
    public string? Namespace { get; set; }
}
