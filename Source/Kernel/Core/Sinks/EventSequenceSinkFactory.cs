// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for <see cref="EventSequenceSink"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>; collaborators are resolved lazily because storage itself depends on the sink factories.</param>
public class EventSequenceSinkFactory(IServiceProvider serviceProvider) : ISinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.EventSequence;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
    {
        readModel.Sink.EnsureReadModelSupported();
        var configuration = readModel.Sink.EventSequence!;
        var schema = readModel.GetTargetSchema();

        // Event compliance runs when the intent is appended and the stored state would be re-read as the next
        // fold's input. Until both sides are specified together, a target carrying compliance metadata is refused.
        if (CarriesCompliance(JsonNode.Parse(schema.ToJson())))
        {
            throw new EventSequenceTargetCarriesCompliance(configuration.EventType);
        }

        return new EventSequenceSink(
            eventStore,
            @namespace,
            readModel,
            configuration,
            serviceProvider.GetRequiredService<IGrainFactory>(),
            serviceProvider.GetRequiredService<IStorage>(),
            serviceProvider.GetRequiredService<IExpandoObjectConverter>());
    }

    static bool CarriesCompliance(JsonNode? node) => node switch
    {
        JsonObject @object => @object.Any(_ => (_.Key == "compliance" && _.Value is JsonArray { Count: > 0 }) || CarriesCompliance(_.Value)),
        JsonArray array => array.Any(CarriesCompliance),
        _ => false
    };
}
