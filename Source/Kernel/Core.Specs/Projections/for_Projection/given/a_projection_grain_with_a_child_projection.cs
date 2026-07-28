// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection_grain_with_a_child_projection : Specification
{
    protected const string EventStore = "the-event-store";
    protected const string ReadModel = "the-read-model";

    protected Projection _grain;
    protected TestKitSilo _silo;
    protected EngineProjection _rootProjection;
    protected EngineProjection _childProjection;
    protected EventType _eventType;
    protected AppendedEvent _event;
    protected Key _rootKey;
    protected Key _childKey;
    protected ProjectionEventContext _rootContext;
    protected ProjectionEventContext _childContext;

    protected ProjectionOperationType RootOperationType { get; set; } = ProjectionOperationType.From | ProjectionOperationType.Join;

    protected ProjectionOperationType ChildOperationType { get; set; } = ProjectionOperationType.From;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        _eventType = new EventType("TheEvent", EventTypeGeneration.First);
        _event = AppendedEvent.EmptyWithEventType(_eventType);
        _rootKey = new Key("the-root-key", ArrayIndexers.NoIndexers);
        _childKey = new Key("the-child-key", ArrayIndexers.NoIndexers);

        _childProjection = Substitute.For<EngineProjection>();
        _childProjection.ChildProjections.Returns([]);
        _childProjection.Accepts(Arg.Any<EventType>()).Returns(true);
        _childProjection.GetOperationTypeFor(Arg.Any<EventType>()).Returns(_ => ChildOperationType);
        _childProjection.GetKeyResolverFor(Arg.Any<EventType>())
            .Returns(new KeyResolver((_, _, _) => Task.FromResult<KeyResolverResult>(new ResolvedKey(_childKey))));

        _rootProjection = Substitute.For<EngineProjection>();
        _rootProjection.ChildProjections.Returns([_childProjection]);
        _rootProjection.Accepts(Arg.Any<EventType>()).Returns(true);
        _rootProjection.GetOperationTypeFor(Arg.Any<EventType>()).Returns(_ => RootOperationType);
        _rootProjection.GetKeyResolverFor(Arg.Any<EventType>())
            .Returns(new KeyResolver((_, _, _) => Task.FromResult<KeyResolverResult>(new ResolvedKey(_rootKey))));

        CaptureContextFrom(_rootProjection, _ => _rootContext = _);
        CaptureContextFrom(_childProjection, _ => _childContext = _);

        var projectionFactory = Substitute.For<IProjectionFactory>();
        projectionFactory.Create(
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<ProjectionDefinition>(),
            Arg.Any<ReadModelDefinition>(),
            Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>()).Returns(_rootProjection);
        _silo.AddService(projectionFactory);

        _silo.AddService(Substitute.For<IProjectionDefinitionComparer>());
        _silo.AddService(Substitute.For<IObjectComparer>());

        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetLatestForAllEventTypes().Returns([]);
        storage.GetEventStore(Arg.Any<EventStoreName>())
            .GetNamespace(Arg.Any<EventStoreNamespaceName>())
            .GetEventSequence(Arg.Any<EventSequenceId>())
            .Returns(Substitute.For<IEventSequenceStorage>());
        _silo.AddService(storage);

        _silo.AddService(Options.Create(new ChronicleOptions()));

        var namespacesGrain = Substitute.For<INamespaces>();
        namespacesGrain.GetAll().Returns([]);
        _silo.AddProbe(_ => namespacesGrain);

        var readModelGrain = Substitute.For<IReadModel>();
        readModelGrain.GetDefinition().Returns(Task.FromResult<ReadModelDefinition>(null!));
        _silo.AddProbe(_ => readModelGrain);

        // ProjectionDefinition has no parameterless constructor, so the test silo cannot materialize
        // an initial state on its own. Seed the storage with the definition the grain starts out with.
        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = CreateDefinition();
        _silo.Options.StorageFactory = _ => stateStorage;

        _grain = await _silo.CreateGrainAsync<Projection>(new ProjectionKey("the-projection", EventStore).ToString());
    }

    protected Task<ExpandoObject> ProcessTheEvent() =>
        _grain.ProcessForSingleReadModel(EventStoreNamespaceName.Default, new ExpandoObject(), [_event]);

    void CaptureContextFrom(EngineProjection projection, Action<ProjectionEventContext> capture) =>
        projection
            .When(_ => _.OnNext(Arg.Any<ProjectionEventContext>()))
            .Do(call => capture(call.Arg<ProjectionEventContext>()));

    static ProjectionDefinition CreateDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "the-projection",
        ReadModel,
        true,
        true,
        new JsonObject(),
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());
}
