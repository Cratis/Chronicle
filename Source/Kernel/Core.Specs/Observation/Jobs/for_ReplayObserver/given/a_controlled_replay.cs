// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.given;

public class a_controlled_replay : a_replay_observer_job
{
    protected readonly EventType event_type = new("fa5b2bb2-318c-4514-8d18-6ef54972151d", EventTypeGeneration.First);
    protected IEventSequenceStorage _eventSequenceStorage;
    protected readonly Key[] _keys = ["first-partition", "second-partition"];
    protected ReplayObserverRequest request;
    protected JobStepDetails[] first_steps;
    protected JobStepDetails[] resumed_steps;

    void Establish()
    {
        var observerKey = _request.ObserverKey;
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _namespaceStorage.GetEventSequence(observerKey.EventSequenceId).Returns(_eventSequenceStorage);
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_keys));
        _observer.GetSubscription().Returns(new ObserverSubscription(observerKey.ObserverId, observerKey, [event_type], typeof(IObserverSubscriber), SiloAddress.Zero)
        {
            TypeSetRevision = new(1),
            TypeSetMembers = new Dictionary<EventTypeId, ObserverTypeSetMember> { [event_type.Id] = new(new(1), null) }
        });
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)100UL);
        request = new(observerKey, ObserverType.Reactor, [event_type]) { ReaderScope = ObserverReaderScope.Explicit([event_type]) };
    }
}
