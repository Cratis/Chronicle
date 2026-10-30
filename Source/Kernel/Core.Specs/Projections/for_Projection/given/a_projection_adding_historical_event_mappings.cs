// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection_adding_historical_event_mappings : a_projection_grain_with_a_versioned_definition
{
    protected readonly EventType _existingEventType = new("ExistingEvent", 1);
    protected readonly EventType _addedEventType = new("NewlyMappedEvent", 1);
    protected ProjectionDefinition _candidate;
    protected INotifyProjectionDefinitionsChanged _subscriber;
    protected IEventSequenceStorage _eventSequence;
    protected Observation.IObserver _observer;

    async Task Establish()
    {
        _stored = _stored with
        {
            AutoMap = AutoMap.Disabled,
            From = new Dictionary<EventType, FromDefinition>
            {
                [_existingEventType] = new(new Dictionary<PropertyPath, string> { ["name"] = "$event.name" }, "$eventSourceId", null)
            }
        };
        _stateStorage.State = _stored;
        _candidate = _stored with
        {
            From = new Dictionary<EventType, FromDefinition>(_stored.From)
            {
                [_addedEventType] = new(new Dictionary<PropertyPath, string> { ["count"] = "$event.count" }, "$eventSourceId", null)
            }
        };
        var comparer = new ObjectComparer();
        _objectComparer.Compare(Arg.Any<object>(), Arg.Any<object>(), Arg.Any<ObjectComparerMode>(), out Arg.Any<IEnumerable<PropertyDifference>>())
            .Returns(call => comparer.Compare(call[0], call[1], call.Arg<ObjectComparerMode>(), out _));
        _comparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        _eventSequence = Substitute.For<IEventSequenceStorage>();
        _eventSequence.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)10UL);
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        var historicalEvent = new AppendedEvent(
            EventContext.Empty with { EventSourceId = "S1", EventType = _addedEventType, SequenceNumber = 10UL },
            new ExpandoObject());
        cursor.Current.Returns([historicalEvent]);
        _eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(cursor);
        _storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>())
            .GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);
        _observer = Substitute.For<Observation.IObserver>();
        SetProjectedEventTypes([_existingEventType, _addedEventType]);
        SetObserverEventTypes([_existingEventType, _addedEventType]);
        _silo.AddProbe(_ => _observer);
        _subscriber = Substitute.For<INotifyProjectionDefinitionsChanged>();
        await _grain.SubscribeDefinitionsChanged(_subscriber);
    }

    protected void SetProjectedEventTypes(IEnumerable<EventType> eventTypes)
    {
        var projection = Substitute.For<Engine.IProjection>();
        projection.EventTypes.Returns(eventTypes);
        _silo.ServiceProvider.GetRequiredService<IProjectionFactory>()
            .Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>())
            .Returns(projection);
    }

    protected void SetObserverEventTypes(IEnumerable<EventType> eventTypes) =>
        _observer.GetDefinition().Returns(ObserverDefinition.Empty with { EventTypes = eventTypes });
}
