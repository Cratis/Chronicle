// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.ObserverRouting;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueShadowRouting;

public class when_comparing : Specification
{
    static readonly EventType _eventType = new("an-event", 1);
    static readonly EventType _otherEventType = new("another-event", 1);
    static readonly ObserverId _agreed = "agreed";
    static readonly ObserverId _onlyInQueue = "only-in-queue";
    static readonly ObserverId _onlyInTable = "only-in-table";
    static readonly ObserverId _onAnotherQueue = "on-another-queue";
    static readonly ObserverId _ofAnotherEventType = "of-another-event-type";

    ObserverRoutingTable _table;
    AppendedEventsQueueObserverSubscription[] _subscriptions;
    AppendedEventsQueueShadowRouting _result;

    void Establish()
    {
        _table = ObserverRoutingTable.From(new ObserverDefinitionRegistrySnapshot(
            1,
            new[]
            {
                DefinitionOf(_agreed, _eventType),
                DefinitionOf(_onlyInTable, _eventType),
                DefinitionOf(_onAnotherQueue, _eventType),
                DefinitionOf(_ofAnotherEventType, _otherEventType)
            }.ToFrozenDictionary(_ => _.Identifier)));
        _subscriptions =
        [
            SubscriptionOf(_agreed, _eventType),
            SubscriptionOf(_onlyInQueue, _eventType),
            SubscriptionOf(_ofAnotherEventType, _otherEventType)
        ];
    }

    void Because() => _result = AppendedEventsQueueShadowRouting.Compare(
        _table,
        EventSequenceId.Log,
        [_eventType.Id, GlobalEventTypes.Redaction],
        _subscriptions,
        observerId => observerId != _onAnotherQueue);

    [Fact] void should_report_the_observer_only_the_queue_takes_the_batch_for() => _result.QueueNotTable.ShouldContainOnly(_onlyInQueue);
    [Fact] void should_report_the_observer_only_the_table_takes_the_batch_for() => _result.TableNotQueue.ShouldContainOnly(_onlyInTable);

    static ObserverDefinition DefinitionOf(ObserverId id, EventType eventType) =>
        new(id, EventTypeSet.From([eventType]), EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true);

    static AppendedEventsQueueObserverSubscription SubscriptionOf(ObserverId id, EventType eventType) =>
        new(new ObserverKey(id, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log), EventTypeSet.From([eventType]));
}
