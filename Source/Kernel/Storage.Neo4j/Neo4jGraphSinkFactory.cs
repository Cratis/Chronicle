// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Graphs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphSinkFactory"/> for <see cref="Neo4jGraphSink"/>.
/// </summary>
/// <remarks>
/// Like every sink factory it resolves its infrastructure lazily, in <see cref="CreateGraphFor"/> rather than in its
/// constructor, so it is safe to keep registered whatever storage backend the kernel runs on.
/// </remarks>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve <see cref="INeo4jConnections"/> from on demand.</param>
public class Neo4jGraphSinkFactory(IServiceProvider serviceProvider) : IGraphSinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Neo4j;

    /// <inheritdoc/>
    public IGraphSink CreateGraphFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) =>
        new Neo4jGraphSink(
            GraphDefinition.FromSchema(readModel.GetSchemaForLatestGeneration()),
            eventStore,
            @namespace,
            serviceProvider.GetRequiredService<INeo4jConnections>());
}
