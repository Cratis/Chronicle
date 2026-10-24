// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Projections;
using Orleans.TestKit;

using IObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// An event sequence with isolated, coupled, unrelated and non-replayable observers.
/// </summary>
public class a_content_mutation_with_observers : an_event_sequence
{
    protected IObserverDefinitionsStorage _observerDefinitions;
    protected IProjectionDefinitionsStorage _projectionDefinitions;
    protected readonly Dictionary<string, IObserver> _observers = [];
    protected readonly List<ObserverDefinition> _definitions = [];
    protected EventType _secondEventType = new("second-mutated-type", EventTypeGeneration.First);
    protected EventType _unchangedEventType = new("unchanged-type", EventTypeGeneration.First);
    protected AppendedEvent _originalEvent;

    protected override EventSequenceId SequenceId => "mutation-sequence";

    void Establish()
    {
        _eventSourceId = "mutated-source-not-a-document-key";
        _originalEvent = new(
            EventContext.From(
                EventStore,
                EventStoreNamespace,
                _eventType,
                EventSourceType.Default,
                _eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                42UL,
                CorrelationId.New()),
            new ExpandoObject());
        _eventSequenceStorage.GetEventAt(42UL).Returns(_originalEvent);
        _eventSequenceStorage.Redact(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<RedactionReason>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<IEnumerable<IdentityId>>(),
            Arg.Any<DateTimeOffset>()).Returns(_originalEvent);
        _eventSequenceStorage.Redact(
            _eventSourceId,
            Arg.Any<RedactionReason>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<IEnumerable<IdentityId>>(),
            Arg.Any<DateTimeOffset>()).Returns([_eventType, _secondEventType]);

        _observerDefinitions = Substitute.For<IObserverDefinitionsStorage>();
        _projectionDefinitions = Substitute.For<IProjectionDefinitionsStorage>();
        _eventStoreStorage.Observers.Returns(_observerDefinitions);
        _eventStoreStorage.Projections.Returns(_projectionDefinitions);
        _observerDefinitions.GetReplayableObserversForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns(call =>
        {
            var types = call.Arg<IEnumerable<EventType>>().ToArray();
            return _definitions.Where(definition => definition.IsReplayable && definition.EventTypes.Intersect(types).Any())
                .SelectMany(definition => new[] { definition, definition }).ToArray();
        });

        AddProjection("implicit", Definition("implicit"));
        AddProjection("explicit", WithKey(Definition("explicit"), "$eventSourceId"));
        AddProjection("arbitrary", WithKey(Definition("arbitrary"), "documentId"));
        AddProjection("derived", Definition("derived") with
        {
            FromDerivatives = [new([_secondEventType], new(new Dictionary<PropertyPath, string>(), "documentId", null))]
        });
        AddProjection("joined", Definition("joined") with
        {
            Join = new Dictionary<EventType, JoinDefinition> { [_secondEventType] = new("id", new Dictionary<PropertyPath, string>(), "documentId") }
        });
        AddProjection("parent", Definition("parent") with
        {
            From = new Dictionary<EventType, FromDefinition> { [_eventType] = new(new Dictionary<PropertyPath, string>(), "", "parentId") }
        });
        AddProjection("children", Definition("children") with
        {
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["children"] = new(
                    "id",
                    new Dictionary<EventType, FromDefinition>(),
                    new Dictionary<EventType, JoinDefinition>(),
                    new Dictionary<PropertyPath, ChildrenDefinition>(),
                    new(new Dictionary<PropertyPath, string>(), false),
                    new Dictionary<EventType, RemovedWithDefinition>(),
                    new Dictionary<EventType, RemovedWithJoinDefinition>())
            }
        });
        AddProjection("legacy", Definition("legacy") with { HasLegacyStorageFields = true });
        AddProjection("unknown", Definition("unknown") with { From = null! });
        AddObserver("missing", ObserverType.Projection);
        _projectionDefinitions.Get((ProjectionId)"missing").Returns(Task.FromResult<ProjectionDefinition>(null!));
        AddObserver("reducer", ObserverType.Reducer);
        AddObserver("reactor", ObserverType.Reactor);
        AddObserver("external", ObserverType.External);
        AddObserver("unrelated", ObserverType.Projection, eventTypes: [_unchangedEventType]);
        AddObserver("non-replayable", ObserverType.Projection, replayable: false);

        var byKey = _observers.ToDictionary(
            pair => new ObserverKey(pair.Key, EventStore, EventStoreNamespace, SequenceId).ToString(),
            pair => pair.Value);
        _silo.AddProbe<IObserver>(identity => byKey[identity.ToString()]);
        _projectionDefinitions.ClearReceivedCalls();
        _storage.ClearReceivedCalls();
    }

    protected async Task ShouldReplayPartition(string id)
    {
        await _observers[id].Received(1).ReplayPartition((Key)_eventSourceId);
        await _observers[id].DidNotReceive().Replay();
        await _observers[id].DidNotReceive().ReplayPartition(Arg.Any<Key>(), Arg.Any<IEnumerable<EventType>>());
    }

    protected async Task ShouldReplayAll(string id)
    {
        await _observers[id].Received(1).Replay();
        await _observers[id].DidNotReceive().ReplayPartition(Arg.Any<Key>());
        await _observers[id].DidNotReceive().ReplayPartition(Arg.Any<Key>(), Arg.Any<IEnumerable<EventType>>());
    }

    protected void ShouldLeaveUntouched(string id) => _observers[id].ReceivedCalls().ShouldBeEmpty();

    protected void ShouldReadOnlyProjectionDefinitions() => _projectionDefinitions.ReceivedCalls()
        .Select(call => call.GetArguments()[0]?.ToString()).Order().ShouldEqual(
            new[] { "arbitrary", "children", "derived", "explicit", "implicit", "joined", "legacy", "missing", "parent", "unknown" }.Order());

    protected void ShouldUseEventStoreScope() => _storage.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(_storage.GetEventStore))
        .ShouldEachConformTo(call => Equals(call.GetArguments()[0], EventStore));

    void AddProjection(string id, ProjectionDefinition definition)
    {
        AddObserver(id, ObserverType.Projection);
        _projectionDefinitions.Get((ProjectionId)id).Returns(definition);
    }

    void AddObserver(string id, ObserverType type, bool replayable = true, IEnumerable<EventType>? eventTypes = null)
    {
        _definitions.Add(new(id, eventTypes ?? [_eventType, _secondEventType, _unchangedEventType], SequenceId, type, ObserverOwner.Client, replayable));
        _observers[id] = Substitute.For<IObserver>();
    }

    protected ProjectionDefinition Definition(string id) => new(
        ProjectionOwner.Client,
        SequenceId,
        id,
        ReadModelIdentifier.NotSet,
        true,
        true,
        new(),
        new Dictionary<EventType, FromDefinition> { [_eventType] = new(new Dictionary<PropertyPath, string>(), "", null) },
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());

    ProjectionDefinition WithKey(ProjectionDefinition definition, PropertyExpression key) => definition with
    {
        From = new Dictionary<EventType, FromDefinition> { [_eventType] = definition.From[_eventType] with { Key = key } }
    };
}
