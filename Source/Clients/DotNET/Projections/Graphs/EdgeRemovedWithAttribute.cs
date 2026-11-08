// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Deletes an edge when an event is observed. The nodes at both ends stay.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <param name="sourceKey">The name of the event property that holds the key of the source node.</param>
/// <param name="targetKey">The name of the event property that holds the key of the target node.</param>
/// <param name="identity">The name of the event property that holds the identity of the edge, for an edge type with an identifying property.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class EdgeRemovedWithAttribute<TEvent>(string sourceKey, string targetKey, string? identity = null) : Attribute, IProjectionAnnotation, IEdgeRemovedWithAttribute
{
    /// <inheritdoc/>
    public Type EventType => typeof(TEvent);

    /// <inheritdoc/>
    public string SourceKey { get; } = sourceKey;

    /// <inheritdoc/>
    public string TargetKey { get; } = targetKey;

    /// <inheritdoc/>
    public string? Identity { get; } = identity;
}
