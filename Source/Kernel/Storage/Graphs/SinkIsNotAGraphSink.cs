// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Exception that gets thrown when a graph sink is asked for from a sink type that does not store a property graph.
/// </summary>
/// <param name="typeId">The <see cref="SinkTypeId"/> of the sink.</param>
public class SinkIsNotAGraphSink(SinkTypeId typeId)
    : Exception($"The sink type '{typeId}' does not store a property graph.");
