// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Settings for observer commands that target a specific observer.
/// </summary>
public class ObserverCommandSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the observer ID.
    /// </summary>
    [CommandArgument(0, "<OBSERVER_ID>")]
    [Description("Observer identifier (from 'cratis observers list')")]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence ID.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence name (default: event-log)")]
    [DefaultValue(CliDefaults.DefaultEventSequenceId)]
    public string EventSequenceId { get; set; } = CliDefaults.DefaultEventSequenceId;
}
