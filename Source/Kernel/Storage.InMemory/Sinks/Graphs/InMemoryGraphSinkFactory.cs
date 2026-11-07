// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Graphs;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphSinkFactory"/> for <see cref="InMemoryGraphSink"/>.
/// </summary>
public class InMemoryGraphSinkFactory : IGraphSinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemoryGraph;

    /// <inheritdoc/>
    public IGraphSink CreateGraphFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) =>
        new InMemoryGraphSink(Concepts.Graphs.GraphDefinition.FromSchema(readModel.GetSchemaForLatestGeneration()));
}
