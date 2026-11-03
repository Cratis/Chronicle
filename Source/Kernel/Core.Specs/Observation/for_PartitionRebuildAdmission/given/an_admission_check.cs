// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission.given;

public class an_admission_check : Specification
{
    protected readonly ObserverKey _key = new("projection", "store", "tenant", EventSequenceId.Log);
    protected IStorage _storage;
    protected IEventStoreStorage _store;
    protected IServiceProvider _services;
    protected IMaterializationKeys _keys;
    protected IMaterializationBindingStorage _bindings;
    protected PartitionRebuildAdmission _admission;
    protected ReplayObserverPartitionRequest _request;
    protected GenerationFloor _floor;
    protected MaterializationKey _materialization;
    protected ReadModelDefinition _readModel;
    protected ISink _sink;
    protected bool _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _store = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore(_key.EventStore).Returns(_store);
        _services = Substitute.For<IServiceProvider>();
        _keys = Substitute.For<IMaterializationKeys>();
        _bindings = Substitute.For<IMaterializationBindingStorage>();
        _services.GetService(typeof(IMaterializationKeys)).Returns(_keys);
        _services.GetService(typeof(IMaterializationBindingStorage)).Returns(_bindings);
        _floor = new(new(1), new(1), [], new(Guid.NewGuid()), 1, DateTimeOffset.MinValue, "test", "test");
        _materialization = new(_floor.Incarnation, _key.Namespace, _key.ObserverId, SinkTypeId.None, new(Guid.Empty));
        _keys.For(_key.EventStore, _key.Namespace, _key.ObserverId).Returns(_materialization);
        _store.GenerationInitialization.GetReceipts().Returns(Array.Empty<GenerationInitializationReceipt>());
        _store.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(null));
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client, EventSequenceId.Log, "projection", ReadModelIdentifier.NotSet, true, true, new(),
            new Dictionary<EventType, FromDefinition> { [new("changed", 1)] = new(new Dictionary<PropertyPath, string>(), new(""), null) },
            new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [],
            new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>());
        _store.Projections.Has(_key.ObserverId).Returns(true);
        _store.Projections.Get(_key.ObserverId).Returns(definition);
        _readModel = new(ReadModelIdentifier.NotSet, new("models"), new("Models"), ReadModelOwner.Client, ReadModelSource.Code,
            ReadModelObserverType.Projection, new("projection"), SinkDefinition.None, new Dictionary<ReadModelGeneration, Cratis.Chronicle.Schemas.JsonSchema>(), []);
        _store.ReadModels.Get(definition.ReadModel).Returns(_readModel);
        _sink = Substitute.For<ISink>();
        _sink.SupportsIsolatedReplace.Returns(true);
        _store.GetNamespace(_key.Namespace).Sinks.GetFor(_readModel).Returns(_sink);
        _request = new(_key, ObserverType.Projection, "partition", 0UL, 50UL, [new EventType("changed", 1)]) { ReplaysAllEventTypes = true };
        _admission = new(Options.Create(new ChronicleOptions { Features = new() { IsolatedPartitionReplay = true } }), _storage, _services, NullLogger<PartitionRebuildAdmission>.Instance);
    }

    protected GenerationInitializationReceipt Receipt() => new(_key.EventStore, GenerationInitializationStep.Enabled, "store", "test", _floor.Incarnation, 1, DateTimeOffset.MinValue);
}
