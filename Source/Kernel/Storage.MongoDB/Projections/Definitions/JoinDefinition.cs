// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the definition for join with a specific event.
/// </summary>
public class JoinDefinition
{
    /// <summary>
    /// Gets or sets the property representing the model property one is joining on.
    /// </summary>
    public required PropertyPath On { get; set; }

    /// <summary>
    /// Gets or sets the properties and expressions for each property.
    /// </summary>
    public required IDictionary<string, string> Properties { get; set; }

    /// <summary>
    /// Gets or sets the key expression, represents the key to use for identifying the model instance.
    /// </summary>
    public required PropertyExpression Key { get; set; }

    /// <summary>
    /// Gets or sets whether properties should be auto-mapped from events.
    /// </summary>
    public Concepts.Projections.Definitions.AutoMap AutoMap { get; set; } = Concepts.Projections.Definitions.AutoMap.Inherit;
}
