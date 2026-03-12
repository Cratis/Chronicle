// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.ReadModels;

/// <summary>
/// Settings for the get read model instances command.
/// </summary>
public class GetReadModelInstancesSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the read model container name.
    /// </summary>
    [CommandArgument(0, "<READ_MODEL>")]
    [Description("The read model container name")]
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [CommandOption("--page <NUMBER>")]
    [Description("Page number (0-based)")]
    [DefaultValue(0)]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [CommandOption("--page-size <SIZE>")]
    [Description("Number of instances per page")]
    [DefaultValue(20)]
    public int PageSize { get; set; } = 20;
}
