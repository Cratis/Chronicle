// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.ReadModels;

/// <summary>
/// Settings for the get read model occurrences command.
/// </summary>
public class GetReadModelOccurrencesSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the read model type identifier.
    /// </summary>
    [CommandArgument(0, "<READ_MODEL_TYPE>")]
    [Description("The read model type identifier")]
    public string ReadModelType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model type generation.
    /// </summary>
    [CommandOption("--generation <NUMBER>")]
    [Description("Read model type generation")]
    [DefaultValue(1u)]
    public uint Generation { get; set; } = 1;
}
