// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.EventTypes;

/// <summary>
/// Settings for the show event type command.
/// </summary>
public class ShowEventTypeSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the event type identifier (name or name+generation).
    /// </summary>
    [CommandArgument(0, "<EVENT_TYPE>")]
    [Description("Event type identifier (name, or name+generation)")]
    public string EventType { get; set; } = string.Empty;
}
