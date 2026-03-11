// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Events;

/// <summary>
/// Settings for the get events command.
/// </summary>
public class GetEventsSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the event sequence ID to query.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence ID")]
    [DefaultValue("event-log")]
    public string EventSequenceId { get; set; } = "event-log";

    /// <summary>
    /// Gets or sets the starting sequence number.
    /// </summary>
    [CommandOption("--from <NUMBER>")]
    [Description("Starting event sequence number")]
    [DefaultValue(0UL)]
    public ulong From { get; set; }

    /// <summary>
    /// Gets or sets the ending sequence number.
    /// </summary>
    [CommandOption("--to <NUMBER>")]
    [Description("Ending event sequence number (optional)")]
    public ulong? To { get; set; }

    /// <summary>
    /// Gets or sets the event source ID filter.
    /// </summary>
    [CommandOption("--event-source-id <ID>")]
    [Description("Filter by event source ID")]
    public string? EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event type filter (name, or name+generation). Defaults to generation 1 if not specified.
    /// </summary>
    [CommandOption("--event-type <TYPE>")]
    [Description("Filter by event type (name, or name+generation)")]
    public string? EventType { get; set; }
}
