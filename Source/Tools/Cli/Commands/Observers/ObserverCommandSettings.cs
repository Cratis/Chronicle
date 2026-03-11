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
    [Description("The observer ID")]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence ID.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence ID")]
    [DefaultValue("event-log")]
    public string EventSequenceId { get; set; } = "event-log";
}
