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
    [Description("Event sequence ID")]
    [DefaultValue("event-log")]
    public string EventSequenceId { get; set; } = "event-log";
}
