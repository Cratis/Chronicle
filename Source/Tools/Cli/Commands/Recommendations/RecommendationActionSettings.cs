// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Recommendations;

/// <summary>
/// Settings for recommendation action commands.
/// </summary>
public class RecommendationActionSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the recommendation ID.
    /// </summary>
    [CommandArgument(0, "<RECOMMENDATION_ID>")]
    [Description("The recommendation ID (GUID)")]
    public Guid RecommendationId { get; set; }
}
