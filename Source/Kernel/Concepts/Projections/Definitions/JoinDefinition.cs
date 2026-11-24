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
/// <param name="AutoMap">Whether same-named event properties should be mapped automatically; inherits the projection setting by default.</param>
[Immutable]
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Projections.Definitions.JoinDefinition")]
public record JoinDefinition(
    [Id(0)] PropertyPath On,
    IReadOnlyDictionary<PropertyPath, string> Properties,
    [Id(2)] PropertyExpression Key,
    [Id(3)] AutoMap AutoMap = AutoMap.Inherit)
{
    /// <summary>
    /// Gets properties and expressions for each property.
    /// </summary>
    [Id(1)]
    public IReadOnlyDictionary<PropertyPath, string> Properties { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(Properties);
}
