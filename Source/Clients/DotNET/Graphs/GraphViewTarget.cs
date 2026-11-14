// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents where a view reads: its graph and its resolved query node.
/// </summary>
/// <param name="Graph">The name of the graph.</param>
/// <param name="Node">The <see cref="GraphQueryNode"/>.</param>
internal sealed record GraphViewTarget(string Graph, GraphQueryNode Node);
