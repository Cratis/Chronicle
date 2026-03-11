// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.FailedPartitions;

/// <summary>
/// Settings for the show failed partition command.
/// </summary>
public class ShowFailedPartitionSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the observer ID.
    /// </summary>
    [CommandArgument(0, "<OBSERVER_ID>")]
    [Description("The observer ID")]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the partition key.
    /// </summary>
    [CommandArgument(1, "<PARTITION>")]
    [Description("The partition key")]
    public string Partition { get; set; } = string.Empty;
}
