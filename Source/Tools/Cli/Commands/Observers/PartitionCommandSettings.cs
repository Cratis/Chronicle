// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Settings for partition-scoped observer commands.
/// </summary>
public class PartitionCommandSettings : ObserverCommandSettings
{
    /// <summary>
    /// Gets or sets the partition key.
    /// </summary>
    [CommandArgument(1, "<PARTITION>")]
    [Description("The partition key")]
    public string Partition { get; set; } = string.Empty;
}
