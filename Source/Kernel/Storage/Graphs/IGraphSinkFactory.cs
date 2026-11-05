// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Graphs;

/// <summary>
/// Defines a factory that can create instances of <see cref="IGraphSink"/> for a specific sink type.
/// </summary>
/// <remarks>
/// A graph sink factory is also a <see cref="ISinkFactory"/>, so it is discovered and registered like every sink, but it
/// does not create document sinks: <see cref="ISinkFactory.CreateFor"/> refuses, and <see cref="ISinks.GetGraphFor"/> is
/// the way to get a graph sink.
/// </remarks>
public interface IGraphSinkFactory : ISinkFactory
{
    /// <summary>
    /// Create a <see cref="IGraphSink"/> for a graph read model.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the sink is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the sink is for.</param>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> whose schema describes the graph.</param>
    /// <returns>A new <see cref="IGraphSink"/>.</returns>
    IGraphSink CreateGraphFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel);

    /// <inheritdoc/>
    ISink ISinkFactory.CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) =>
        throw new GraphSinkIsNotAReadModelSink(TypeId);
}
