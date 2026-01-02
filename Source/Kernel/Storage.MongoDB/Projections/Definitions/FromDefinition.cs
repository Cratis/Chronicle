// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the definition from for a specific event.
/// </summary>
public class FromDefinition
{
    /// <summary>
    /// Gets or sets the properties and expressions for each property.
    /// </summary>
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the key expression, represents the key to use for identifying the model instance.
    /// </summary>
    public PropertyExpression Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional parent key expression, typically used in child relationships for identifying parent read model.
    /// </summary>
    public PropertyExpression? ParentKey { get; set; }

    /// <summary>
    /// Gets or sets whether properties should be auto-mapped from events.
    /// </summary>
    public Concepts.Projections.Definitions.AutoMap AutoMap { get; set; } = Concepts.Projections.Definitions.AutoMap.Inherit;
}
