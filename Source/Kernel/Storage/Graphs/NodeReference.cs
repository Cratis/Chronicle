// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Represents a reference to a node.
/// </summary>
/// <param name="Label">The label of the node.</param>
/// <param name="Key">The key of the node. Always a string, a whole number, a decimal number or a boolean.</param>
public sealed record NodeReference(string Label, object Key) : GraphElementReference;
