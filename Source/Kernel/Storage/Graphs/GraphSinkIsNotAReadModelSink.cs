// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Exception that gets thrown when a graph sink is asked for as a document or relational read model sink.
/// </summary>
/// <param name="typeId">The <see cref="SinkTypeId"/> of the graph sink.</param>
public class GraphSinkIsNotAReadModelSink(SinkTypeId typeId)
    : Exception($"The sink type '{typeId}' stores a property graph. It cannot be used as a document or relational read model sink.");
