// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_a_new_event_type_has_history_behind_the_watermark : given.a_projection_grain_with_a_versioned_definition
{
    readonly EventType _existingEventType = new("ExistingEvent", 1);
    readonly EventType _addedEventType = new("NewlyMappedEvent", 1);
    ProjectionDefinition _candidate;
    ProjectionDefinition _result;
    INotifyProjectionDefinitionsChanged _subscriber;

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

        var eventSequence = Substitute.For<IEventSequenceStorage>();
        eventSequence.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)10UL);
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        var historicalEvent = new AppendedEvent(
            EventContext.Empty with { EventSourceId = "S1", EventType = _addedEventType, SequenceNumber = 10UL },
            new ExpandoObject());
        cursor.Current.Returns([historicalEvent]);
        eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(cursor);
        _storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>())
            .GetEventSequence(Arg.Any<EventSequenceId>()).Returns(eventSequence);
        _subscriber = Substitute.For<INotifyProjectionDefinitionsChanged>();
        await _grain.SubscribeDefinitionsChanged(_subscriber);
    }

    async Task Because()
    {
        await Catch.Exception(() => _grain.SetDefinition(_candidate));
        _result = await _grain.GetDefinition();
    }

    [Fact] void should_keep_the_previously_applied_definition() => _result.ShouldBeSame(_stored);
    [Fact] void should_not_persist_the_unapplied_candidate() => _stateStorage.DidNotReceive().WriteStateAsync();
    [Fact] void should_not_notify_subscribers_of_the_unapplied_candidate() => _subscriber.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_automatically_schedule_partial_replay() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
