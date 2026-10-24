// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_pinned_projection_grain : Specification
{
    protected Projection _grain;
    protected EngineProjection _projection;
    protected EventType _pin;
    protected AppendedEvent _stored;
    protected AppendedEvent _released;
    protected AppendedEvent _resolved;
    protected ProjectionEventContext _projected;
    protected ProjectionDefinition _definition;
    protected ProjectionDefinition _engineDefinition;

    protected virtual EventGenerationDelivery Delivery => EventGenerationDelivery.Pinned;
    protected virtual EventTypeGeneration PinGeneration => EventTypeGeneration.First;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _pin = new EventType("TheEvent", PinGeneration);
        _stored = AppendedEvent.EmptyWithEventType(_pin with { Generation = new EventTypeGeneration(2) });
        dynamic content = new ExpandoObject();
        content.name = "released";
        _released = AppendedEvent.EmptyWithEventType(_pin) with { Content = (ExpandoObject)content };
        var key = new Key("the-key", ArrayIndexers.NoIndexers);

        _projection = Substitute.For<EngineProjection>();
        _projection.GenerationDelivery.Returns(Delivery);
        _projection.EventTypes.Returns([_pin]);
        _projection.ReleaseEvent(_pin, Arg.Any<AppendedEvent>()).Returns(_released);
        _projection.ChildProjections.Returns([]);
        _projection.Accepts(Arg.Any<EventType>()).Returns(true);
        _projection.GetOperationTypeFor(Arg.Any<EventType>()).Returns(ProjectionOperationType.From);
        _projection.GetKeyResolverFor(Arg.Any<EventType>()).Returns(new KeyResolver((_, _, @event) =>
        {
            _resolved = @event;
            return Task.FromResult<KeyResolverResult>(new ResolvedKey(key));
        }));
        _projection.InitialModelState.Returns(new ExpandoObject());
        var schema = new JsonSchema();
        schema.Properties["id"] = new JsonSchemaProperty("id", new JsonObject { ["type"] = "string" }, schema);
        _projection.TargetReadModelSchema.Returns(schema);
        _projection.When(_ => _.OnNext(Arg.Any<ProjectionEventContext>()))
            .Do(call => _projected = call.Arg<ProjectionEventContext>());

        var factory = Substitute.For<IProjectionFactory>();
        factory.Create(
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<ProjectionDefinition>(),
            Arg.Any<ReadModelDefinition>(),
            Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>()).Returns(call =>
        {
            _engineDefinition = call.Arg<ProjectionDefinition>();
            return _projection;
        });
        silo.AddService(factory);
        silo.AddService(Substitute.For<IProjectionDefinitionComparer>());
        silo.AddService(Substitute.For<IObjectComparer>());
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetLatestForAllEventTypes().Returns([]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>())
            .GetEventSequence(Arg.Any<EventSequenceId>()).Returns(Substitute.For<IEventSequenceStorage>());
        silo.AddService(storage);
        silo.AddService(Options.Create(new ChronicleOptions()));
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([]);
        silo.AddProbe(_ => namespaces);
        var readModel = Substitute.For<IReadModel>();
        readModel.GetDefinition().Returns(Task.FromResult<ReadModelDefinition>(null!));
        silo.AddProbe(_ => readModel);

        _definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "the-projection",
            "the-read-model",
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition> { [_pin] = new(new Dictionary<PropertyPath, string>(), "$eventSourceId", null) },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>())
        {
            GenerationDelivery = Delivery
        };
        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = _definition;
        silo.Options.StorageFactory = _ => stateStorage;
        _grain = await silo.CreateGrainAsync<Projection>(new ProjectionKey("the-projection", "the-event-store").ToString());
    }

    protected Task<SingleReadModelProjectionState> Fold() =>
        _grain.FoldForSingleReadModel(EventStoreNamespaceName.Default, SingleReadModelProjectionState.Empty, [_stored]);
}
