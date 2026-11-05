// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Represents a page of nodes or edges read from a graph.
/// </summary>
/// <typeparam name="TElement">The type of element, <see cref="GraphNode"/> or <see cref="GraphEdge"/>.</typeparam>
/// <param name="Elements">The elements on the page.</param>
/// <param name="TotalCount">The number of elements of the label or type in total.</param>
public record GraphElements<TElement>(IReadOnlyList<TElement> Elements, long TotalCount);
