// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition for a collection of property actions to perform for all events in the projection.
/// </summary>
/// <param name="Properties">Properties and expressions for each property.</param>
/// <param name="IncludeChildren">Include event types from child projections.</param>
public record FromEveryDefinition(IDictionary<PropertyPath, string> Properties, bool IncludeChildren)
{
    /// <summary>
    /// Gets or sets whether properties should be auto-mapped from events.
    /// </summary>
    public AutoMap AutoMap { get; set; } = AutoMap.Inherit;
}

