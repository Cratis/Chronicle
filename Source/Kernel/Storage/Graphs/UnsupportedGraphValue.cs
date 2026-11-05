// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Exception that gets thrown when a value that is not a primitive or a list of primitives is written to a graph.
/// </summary>
/// <param name="value">The value.</param>
public class UnsupportedGraphValue(object value)
    : Exception($"A graph stores primitives and lists of primitives only, not '{value.GetType().FullName}'.");
