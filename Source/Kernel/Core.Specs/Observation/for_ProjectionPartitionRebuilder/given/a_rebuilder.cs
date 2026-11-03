// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using Orleans.TestKit;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;
using EngineProjectionsManager = Cratis.Chronicle.Projections.Engine.IProjectionsManager;

namespace Cratis.Chronicle.Observation.for_ProjectionPartitionRebuilder.given;

public class a_rebuilder : Specification
{
    protected readonly ObserverKey _observerKey = new("the-projection", "the-store", "the-namespace", EventSequenceId.Log);
    protected readonly Key _partition = new("the-partition", ArrayIndexers.NoIndexers);
    protected ProjectionPartitionRebuilder _rebuilder;
    protected EngineProjectionsManager _engineProjections;
    protected EngineProjection _projection;
    protected IProjectionPipelineManager _pipelines;
    protected IProjectionPipeline _pipeline;
    protected ISink _isolatedSink;
    protected ISink _liveSink;
    protected bool _eventsRemoveTheModel;
    protected IPartitionRebuildAdmission _admission;
    protected Storage.IStorage _storage;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _projection = Substitute.For<EngineProjection>();
        _projection.Accepts(Arg.Any<EventType>()).Returns(true);
        _engineProjections = Substitute.For<EngineProjectionsManager>();
        _engineProjections.GetCurrent(_observerKey.EventStore, _observerKey.Namespace, _observerKey.ObserverId).Returns(_projection);
        silo.AddService(_engineProjections);

        _pipeline = Substitute.For<IProjectionPipeline>();
        _pipeline.Handle(Arg.Any<AppendedEvent>()).Returns(async call => await Handle((AppendedEvent)call[0]));
        _pipelines = Substitute.For<IProjectionPipelineManager>();
        _pipelines.CreateIsolated(_observerKey.EventStore, _observerKey.Namespace, _projection, Arg.Any<ISink>()).Returns(call =>
        {
            _isolatedSink = (ISink)call[3];
            return _pipeline;
        });
        silo.AddService(_pipelines);

        _liveSink = Substitute.For<ISink>();
        _storage = Substitute.For<Storage.IStorage>();
        _storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Sinks.GetFor(Arg.Any<ReadModelDefinition>()).Returns(_liveSink);
        silo.AddService(_storage);
        _admission = Substitute.For<IPartitionRebuildAdmission>();
        _admission.IsAdmitted(Arg.Any<Jobs.ReplayObserverPartitionRequest>()).Returns(true);
        silo.AddService(_admission);

        _rebuilder = await silo.CreateGrainAsync<ProjectionPartitionRebuilder>(Guid.NewGuid().ToString());
    }

    protected static AppendedEvent EventAt(ulong sequenceNumber) =>
        AppendedEvent.EmptyWithEventSequenceNumber(sequenceNumber) with
        {
            Context = EventContext.Empty with { SequenceNumber = sequenceNumber, EventSourceId = "the-partition" }
        };

    async Task<ProjectionEventContext> Handle(AppendedEvent @event)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), @event, new ExpandoObject());
        if (_eventsRemoveTheModel)
        {
            changeset.Remove();
        }
        else
        {
            var next = new ExpandoObject();
            ((IDictionary<string, object?>)next)["name"] = $"version-{@event.Context.SequenceNumber}";
            new ObjectComparer().Compare(changeset.InitialState, next, out var differences);
            changeset.ReplaceState(next, differences);
        }

        await _isolatedSink.ApplyChanges(_partition, changeset, @event.Context.SequenceNumber);
        return new ProjectionEventContext(_partition, @event, changeset, ProjectionOperationType.From, false);
    }
}
