// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents the views declared over one property graph. This is a transport and conformance form, produced from the
/// declared views and never written by hand.
/// </summary>
/// <param name="Graph">The name of the graph.</param>
/// <param name="Views">The <see cref="GraphView">views</see>, ordered by name.</param>
/// <param name="Schema">The <see cref="GraphViewSchema"/> the views were checked against, when known.</param>
public sealed record GraphViewDefinition(string Graph, IReadOnlyList<GraphView> Views, GraphViewSchema? Schema = null);
