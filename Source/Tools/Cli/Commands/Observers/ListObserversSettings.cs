// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Observers;

/// <summary>
/// Settings for the list observers command.
/// </summary>
public class ListObserversSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the observer type filter (reactor, reducer, projection, or all).
    /// </summary>
    [CommandOption("-t|--type <TYPE>")]
    [Description("Filter by observer type: reactor, reducer, projection, or all")]
    [DefaultValue("all")]
    public string Type { get; set; } = "all";
}
