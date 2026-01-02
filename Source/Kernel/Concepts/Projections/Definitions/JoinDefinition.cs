// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition for join with a specific event.
/// </summary>
/// <param name="On">The property representing the model property one is joining on.</param>
/// <param name="Properties">Properties and expressions for each property.</param>
/// <param name="Key">Key expression, represents the key to use for identifying the model instance.</param>
public record JoinDefinition(PropertyPath On, IDictionary<PropertyPath, string> Properties, PropertyExpression Key)
{
    /// <summary>
    /// Gets or sets whether properties should be auto-mapped from events.
    /// </summary>
    public AutoMap AutoMap { get; set; } = AutoMap.Inherit;
}

