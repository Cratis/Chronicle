// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.ObserverRouting;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.given;

public class a_router_and_a_table : Specification
{
    protected const int QueueCount = 4;
    protected static readonly EventType _typed = new("typed-event", 1);
    protected static readonly EventType _unobserved = new("unobserved-event", 1);

    protected AppendedEventsQueueRouter _router;
    protected ObserverId _typedObserver;
    protected ObserverId _everythingObserver;
    protected ObserverRoutingTable _table;

    void Establish()
    {
        _router = new(QueueCount);
        _typedObserver = ObserverOnQueue(1);
        _everythingObserver = ObserverOnQueue(3);
        _table = ObserverRoutingTable.From(new ObserverDefinitionRegistrySnapshot(
            1,
            new[]
            {
                new ObserverDefinition(_typedObserver, EventTypeSet.From([_typed]), EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true),
                new ObserverDefinition(_everythingObserver, EventTypeSet.From([]), EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true)
            }.ToFrozenDictionary(_ => _.Identifier)));
    }

    ObserverId ObserverOnQueue(int queueIndex) =>
        Enumerable.Range(0, 1000).Select(_ => new ObserverId($"observer-{_}")).First(_ => _router.GetQueueIndexFor(_) == queueIndex);
}
