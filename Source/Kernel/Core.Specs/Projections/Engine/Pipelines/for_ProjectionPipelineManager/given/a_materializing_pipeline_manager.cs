// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned.given;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.given;

public class a_materializing_pipeline_manager : a_projection_with_generations
{
    protected readonly EventStoreName _store = "store";
    protected readonly EventStoreNamespaceName _namespace = "namespace";
    protected readonly ReplayContext _replay = new(new("model", 1), "model", "rebuild", DateTimeOffset.UtcNow);
    protected readonly Dictionary<string, ExpandoObject> _saved = [];
    protected ProjectionPipelineManager _manager;
    protected IStorage _managerStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IChangesetStorage _sharedChangesets;
    protected IProjectionFutures _sharedFutures;
    protected IGrainFactory _grainFactory;
    protected ISink _liveSink;
    protected ISink _generationSink;
    protected MaterializationGenerationIdentity _generation;

    async Task Establish()
    {
        _definition = _definition with
        {
            From = _definition.From.ToDictionary(_ => _.Key, _ => _.Value with { Key = WellKnownExpressions.EventSourceId })
        };
        _projection = await _factory.Create(_store, _namespace, _definition, _readModel, _schemas);
        _managerStorage = Substitute.For<IStorage>();
        _namespaceStorage = _managerStorage.GetEventStore(_store).GetNamespace(_namespace);
        _namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _sharedChangesets = Substitute.For<IChangesetStorage>();
        _namespaceStorage.Changesets.Returns(_sharedChangesets);
        _sharedFutures = Substitute.For<IProjectionFutures>();
        _sharedFutures.GetFutures().Returns(Task.FromResult<IEnumerable<ProjectionFuture>>([]));
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IProjectionFutures>(Arg.Any<string>(), Arg.Any<string?>()).Returns(_sharedFutures);
        _liveSink = Substitute.For<ISink>();
        _generationSink = Substitute.For<ISink>();
        _liveSink.FindOrDefault(Arg.Any<Key>()).Returns(call => _saved.GetValueOrDefault(call.Arg<Key>().ToString()));
        _liveSink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>())
            .Returns(call =>
            {
                _saved[call.Arg<Key>().ToString()] = call.Arg<IChangeset<AppendedEvent, ExpandoObject>>().CurrentState;
                return [];
            });
        _namespaceStorage.Sinks.GetFor(_readModel).Returns(_liveSink);
        _generation = MaterializationGenerationIdentity.For(_projection.Identifier, 2U, _readModel.ContainerName);
        _namespaceStorage.Sinks.GetFor(_readModel, _generation.Destination).Returns(_generationSink);
        _manager = new(
            _managerStorage,
            _grainFactory,
            new ObjectComparer(),
            new TypeFormats(),
            Substitute.For<IReadModelsCompliance>(),
            Options.Create(new Configuration.ChronicleOptions()),
            NullLoggerFactory.Instance);
        ClearSharedCalls();
    }

    protected void ClearSharedCalls()
    {
        _namespaceStorage.ClearReceivedCalls();
        _sharedChangesets.ClearReceivedCalls();
        _sharedFutures.ClearReceivedCalls();
        _grainFactory.ClearReceivedCalls();
        _liveSink.ClearReceivedCalls();
    }

    protected AppendedEvent Incoming(EventType type, string name, EventSequenceNumber sequenceNumber) =>
        Event(type, name) with { Context = EventContext.EmptyWithEventSourceId("source") with { EventType = type, SequenceNumber = sequenceNumber } };
}
