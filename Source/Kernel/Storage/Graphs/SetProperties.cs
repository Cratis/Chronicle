// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Sets properties on a node or an edge that already exists. Nothing is created when the element does not exist.
/// </summary>
/// <param name="Element">The <see cref="GraphElementReference"/> of the node or edge.</param>
/// <param name="Properties">The properties to set.</param>
public sealed record SetProperties(GraphElementReference Element, IReadOnlyDictionary<string, object?> Properties) : GraphOperation;
