// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Json;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

using ClientComplianceMetadataResolver = Client::Cratis.Chronicle.Compliance.IComplianceMetadataResolver;
using ClientJsonSchemaGenerator = Client::Cratis.Chronicle.Schemas.JsonSchemaGenerator;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

/// <summary>
/// Runs the passive read path with generated schemas, the real Projection grain and engine, and the real
/// ExpandoObjectConverter. Only Orleans routing, read model registration and the event cursor are substituted.
/// </summary>
public class a_passive_projection_with_nullable_concepts : Specification
{
    protected const string ThingKey = "thing-1";
    protected const string ConfiguredEndpoint = "https://example.com";
    protected const string ConfiguredName = "Configured thing";
    const string EventStore = "the-event-store";
    const string ProjectionId = "the-projection";
    const string ReadModelId = "the-read-model";

    protected ImmediateProjection _grain;
    protected JsonSerializerOptions _serializerOptions;

    async Task Establish()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new ConceptAsJsonConverterFactory() }
        };
        var generator = new ClientJsonSchemaGenerator(Substitute.For<ClientComplianceMetadataResolver>(), new CamelCaseNamingPolicy());
        var readModelSchema = generator.GenerateForReadModel(typeof(Thing));
        var eventSchema = generator.Generate(typeof(ThingConfigured));
        var eventType = new EventType(nameof(ThingConfigured), EventTypeGeneration.First);
        var readModelDefinition = new ReadModelDefinition(
            ReadModelId,
            ReadModelId,
            "Thing",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ProjectionId,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = readModelSchema },
            []);
        var definition = CreateProjectionDefinition(eventType);
        var typeFormats = new TypeFormats();
        var converter = new ExpandoObjectConverter(typeFormats);
        var configuredEvent = new ThingConfigured(new Endpoint(ConfiguredEndpoint), ConfiguredName);
        var appendedEvent = AppendedEvent.EmptyWithEventType(eventType) with
        {
            Context = AppendedEvent.EmptyWithEventType(eventType).Context with
            {
                EventSourceId = ThingKey,
                SequenceNumber = EventSequenceNumber.First
            },
            Content = converter.ToExpandoObject(JsonSerializer.SerializeToNode(configuredEvent, _serializerOptions)!.AsObject(), eventSchema)
        };
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns([appendedEvent]);
        cursor.MoveNext().Returns(true, false);
        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ThingKey, eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(cursor);

        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetLatestForAllEventTypes().Returns(
            [new EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, eventSchema)]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>()).GetEventSequence(EventSequenceId.Log).Returns(eventSequenceStorage);
        var readModel = Substitute.For<IReadModel>();
        readModel.GetDefinition().Returns(readModelDefinition);
        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = definition;

        // Separate TestKit silos keep each grain's identity intact while routing the immediate grain's
        // IProjection calls directly to the real Projection grain.
        var projectionSilo = new TestKitSilo();
        projectionSilo.AddService(storage);
        projectionSilo.AddService(Substitute.For<IProjectionDefinitionComparer>());
        projectionSilo.AddService<IObjectComparer>(new ObjectComparer());
        projectionSilo.AddService(Options.Create(new ChronicleOptions()));
        projectionSilo.AddService<ILogger<Projection>>(NullLogger<Projection>.Instance);
        projectionSilo.AddService<IProjectionFactory>(CreateFactory(storage, typeFormats, converter));
        projectionSilo.AddProbe(_ => readModel);
        projectionSilo.Options.StorageFactory = _ => stateStorage;
        var projection = await projectionSilo.CreateGrainAsync<Projection>(new ProjectionKey(ProjectionId, EventStore).ToString());

        var immediateSilo = new TestKitSilo();
        immediateSilo.AddService(storage);
        immediateSilo.AddService<IExpandoObjectConverter>(converter);
        immediateSilo.AddService<ILogger<ImmediateProjection>>(NullLogger<ImmediateProjection>.Instance);
        immediateSilo.AddProbe<IProjection>(_ => projection);
        immediateSilo.AddProbe(_ => readModel);
        var eventSequence = Substitute.For<EventSequences.IEventSequence>();
        eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.First);
        immediateSilo.AddProbe(_ => eventSequence);
        immediateSilo.Options.StorageFactory = _ => stateStorage;
        _grain = await immediateSilo.CreateGrainAsync<TestableImmediateProjection>(
            new ImmediateProjectionKey(ProjectionId, EventStore, EventStoreNamespaceName.Default, EventSequenceId.Log, ThingKey).ToString());
    }

    static ProjectionFactory CreateFactory(Storage.IStorage storage, TypeFormats typeFormats, IExpandoObjectConverter converter)
    {
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventValueResolvers = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        return new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(eventValueResolvers, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            eventValueResolvers,
            new KeyExpressionResolvers(eventValueResolvers, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            converter,
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance);
    }

    /// <summary>
    /// Creates the kernel definition equivalent of Thing's [Passive], [FromEvent] and [SetFrom] attributes.
    /// </summary>
    /// <param name="eventType">The configuring event type.</param>
    /// <returns>The passive projection definition.</returns>
    static ProjectionDefinition CreateProjectionDefinition(EventType eventType) => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        ProjectionId,
        ReadModelId,
        false,
        true,
        new JsonObject(),
        new Dictionary<EventType, FromDefinition>
        {
            [eventType] = new(
                new Dictionary<PropertyPath, string> { ["endpoint"] = "endpoint", ["name"] = "name" },
                WellKnownExpressions.EventSourceId,
                null)
        },
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());
}
