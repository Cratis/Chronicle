// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Marks the member of an edge that holds the key of the node the edge ends at.
/// </summary>
/// <typeparam name="TNode">The type of the node.</typeparam>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class TargetAttribute<TNode> : Attribute, IGraphTargetAttribute
{
    /// <inheritdoc/>
    public Type NodeType => typeof(TNode);
}
