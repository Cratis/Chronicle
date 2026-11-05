// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Graphs;

namespace Cratis.Chronicle.Services.Projections.Definitions;

/// <summary>
/// Converts graph projection definitions between the contract and the kernel representation.
/// </summary>
internal static class GraphProjectionDefinitionConverters
{
    /// <summary>
    /// Convert to the contract.
    /// </summary>
    /// <param name="definition">The kernel definition.</param>
    /// <returns>The contract.</returns>
    public static Contracts.Projections.GraphProjectionDefinition ToContract(this GraphProjectionDefinition definition) => new()
    {
        Nodes = definition.Nodes.ToDictionary(_ => _.Key, _ => new Contracts.Projections.GraphNodeProjectionDefinition
        {
            From = _.Value.From.ToDictionary(from => from.Key, from => new Contracts.Projections.GraphNodeFromDefinition
            {
                Key = from.Value.Key,
                Properties = new Dictionary<string, string>(from.Value.Properties),
                UpdateOnly = from.Value.UpdateOnly
            }),
            RemovedWith = _.Value.RemovedWith.ToDictionary(removal => removal.Key, removal => new Contracts.Projections.GraphNodeRemovedWithDefinition
            {
                Key = removal.Value.Key,
                Detach = removal.Value.Detach
            })
        }),
        Edges = definition.Edges.ToDictionary(_ => _.Key, _ => new Contracts.Projections.GraphEdgeProjectionDefinition
        {
            From = _.Value.From.ToDictionary(from => from.Key, from => new Contracts.Projections.GraphEdgeFromDefinition
            {
                SourceKey = from.Value.SourceKey,
                TargetKey = from.Value.TargetKey,
                Identity = from.Value.Identity?.Value,
                Properties = new Dictionary<string, string>(from.Value.Properties)
            }),
            RemovedWith = _.Value.RemovedWith.ToDictionary(removal => removal.Key, removal => new Contracts.Projections.GraphEdgeRemovedWithDefinition
            {
                SourceKey = removal.Value.SourceKey,
                TargetKey = removal.Value.TargetKey,
                Identity = removal.Value.Identity?.Value
            })
        })
    };

    /// <summary>
    /// Convert to the kernel definition.
    /// </summary>
    /// <param name="contract">The contract.</param>
    /// <returns>The kernel definition.</returns>
    public static GraphProjectionDefinition ToChronicle(this Contracts.Projections.GraphProjectionDefinition contract) => new(
        contract.Nodes.ToDictionary(_ => _.Key, _ => new GraphNodeProjectionDefinition(
            _.Value.From.ToDictionary(from => from.Key, from => new GraphNodeFromDefinition(
                from.Value.Key,
                new Dictionary<string, string>(from.Value.Properties),
                from.Value.UpdateOnly)),
            _.Value.RemovedWith.ToDictionary(removal => removal.Key, removal => new GraphNodeRemovedWithDefinition(removal.Value.Key, removal.Value.Detach)))),
        contract.Edges.ToDictionary(_ => _.Key, _ => new GraphEdgeProjectionDefinition(
            _.Value.From.ToDictionary(from => from.Key, from => new GraphEdgeFromDefinition(
                from.Value.SourceKey,
                from.Value.TargetKey,
                Expression(from.Value.Identity),
                new Dictionary<string, string>(from.Value.Properties))),
            _.Value.RemovedWith.ToDictionary(removal => removal.Key, removal => new GraphEdgeRemovedWithDefinition(removal.Value.SourceKey, removal.Value.TargetKey, Expression(removal.Value.Identity))))));

    static PropertyExpression? Expression(string? expression) => expression is null ? null : new PropertyExpression(expression);
}
