// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a node of a <see cref="GraphViewSchema"/>.
/// </summary>
/// <param name="Label">The node label.</param>
/// <param name="Key">The name of the key property.</param>
/// <param name="Properties">The names of the properties, the key included, in declaration order.</param>
public sealed record GraphViewSchemaNode(string Label, string Key, IReadOnlyList<string> Properties);
