// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Events;

/// <summary>
/// Settings for the has events command.
/// </summary>
public class HasEventsSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the event source ID to check.
    /// </summary>
    [CommandArgument(0, "<EVENT_SOURCE_ID>")]
    [Description("The event source ID to check for events")]
    public string EventSourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence ID.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence name (default: event-log)")]
    [DefaultValue(CliDefaults.DefaultEventSequenceId)]
    public string EventSequenceId { get; set; } = CliDefaults.DefaultEventSequenceId;
}
