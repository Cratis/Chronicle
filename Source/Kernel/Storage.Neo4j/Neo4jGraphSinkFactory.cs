// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="IGraphSinkFactory"/> for <see cref="Neo4jGraphSink"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get the <see cref="INeo4jConnections"/> from.</param>
public class Neo4jGraphSinkFactory(IServiceProvider serviceProvider) : IGraphSinkFactory
{
    readonly ConcurrentDictionary<string, INeo4jConnections> _namedConnections = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.Neo4j;

    /// <inheritdoc/>
    public IGraphSink CreateGraphFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) =>
        Create(eventStore, @namespace, readModel, serviceProvider.GetRequiredService<INeo4jConnections>());

    /// <inheritdoc/>
    public IGraphSink CreateGraphFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, SinkName sink) =>
        sink.IsDefault
            ? CreateGraphFor(eventStore, @namespace, readModel)
            : Create(eventStore, @namespace, readModel, ConnectionsFor(sink));

    /// <summary>
    /// Get the settings of a named Neo4j sink: its own server and authentication, resolved through the kernel's connection resolution.
    /// </summary>
    /// <param name="kernel">The <see cref="ChronicleOptions"/> of the kernel.</param>
    /// <param name="configuration">The <see cref="SinkConfiguration"/> of the sink.</param>
    /// <returns>The <see cref="ChronicleOptions"/> the sink's connections are made with.</returns>
    internal static ChronicleOptions OptionsFor(ChronicleOptions kernel, SinkConfiguration configuration) => new()
    {
        Neo4j = configuration.ToNeo4jOptions(),
        Storage = new() { ConnectionResolution = kernel.Storage.ConnectionResolution }
    };

    static Neo4jGraphSink Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, INeo4jConnections connections) =>
        new(GraphDefinition.FromSchema(readModel.GetSchemaForLatestGeneration()), eventStore, @namespace, connections);

    INeo4jConnections ConnectionsFor(SinkName sink)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        if (!options.Sinks.TryGet(sink.Value, out var key, out var configuration) ||
            !string.Equals(configuration.Type, WellKnownSinkTypes.Neo4j.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new SinkTypeDoesNotSupportNamedConfigurations(WellKnownSinkTypes.Neo4j, sink);
        }

        return _namedConnections.GetOrAdd(
            key,
            static (_, state) => new Neo4jConnections(
                Options.Create(OptionsFor(state.Options, state.Configuration)),
                state.ServiceProvider.GetRequiredService<IConfiguration>(),
                state.ServiceProvider.GetRequiredService<INeo4jDriverFactory>(),
                state.ServiceProvider),
            (ServiceProvider: serviceProvider, Options: options, Configuration: configuration));
    }
}
