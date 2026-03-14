// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.ReadModels;

/// <summary>
/// Settings for read model commands that target a specific instance by key.
/// </summary>
public class ReadModelKeySettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the read model identifier.
    /// </summary>
    [CommandArgument(0, "<READ_MODEL>")]
    [Description("Read model container name (from 'cratis read-models list')")]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model key.
    /// </summary>
    [CommandArgument(1, "<KEY>")]
    [Description("Read model instance key (typically an event source ID)")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence ID.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence name (default: event-log)")]
    [DefaultValue(CliDefaults.DefaultEventSequenceId)]
    public string EventSequenceId { get; set; } = CliDefaults.DefaultEventSequenceId;
}
