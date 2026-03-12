// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Events;

/// <summary>
/// Settings for the count events command.
/// </summary>
public class CountEventsSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the event sequence ID.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence name (default: event-log)")]
    [DefaultValue(CliDefaults.DefaultEventSequenceId)]
    public string EventSequenceId { get; set; } = CliDefaults.DefaultEventSequenceId;

    /// <summary>
    /// Gets or sets the event type filter (comma-separated; each can be name or name+generation).
    /// </summary>
    [CommandOption("--event-type <TYPE>")]
    [Description("Filter by event type (e.g. UserRegistered or UserRegistered+1). Comma-separate multiple.")]
    public string? EventType { get; set; }

    /// <summary>
    /// Gets or sets the event source ID filter.
    /// </summary>
    [CommandOption("--event-source-id <ID>")]
    [Description("Filter by event source ID")]
    public string? EventSourceId { get; set; }
}
