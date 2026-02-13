// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the definition for a collection of property actions to perform for all events in the projection.
/// </summary>
public class FromEveryDefinition
{
    /// <summary>
    /// Gets or sets the properties and expressions for each property.
    /// </summary>
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets whether to include event types from child projections.
    /// </summary>
    public bool IncludeChildren { get; set; }

    /// <summary>
    /// Gets or sets whether properties should be auto-mapped from events.
    /// </summary>
    public Concepts.Projections.Definitions.AutoMap AutoMap { get; set; } = Concepts.Projections.Definitions.AutoMap.Inherit;
}
