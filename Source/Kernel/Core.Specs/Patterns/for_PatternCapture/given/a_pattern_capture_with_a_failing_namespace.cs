// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.given;

public class a_pattern_capture_with_a_failing_namespace : a_pattern_capture
{
    protected IObserver _failedObserver;

    void Establish()
    {
        EventTypesAre("CustomerNamed");
        _namespaces.GetAll().Returns([_namespace, new EventStoreNamespaceName("healthy-namespace")]);
        _failedObserver = Substitute.For<IObserver>();
        var key = new ObserverKey(PatternCapture.ObserverIdentifier, _eventStore, _namespace, EventSequenceId.Log);
        _grainFactory.GetGrain<IObserver>(key.ToString(), Arg.Any<string>()).Returns(_failedObserver);
        _failedObserver.SubscribeAdditively<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false)
            .Returns(Task.FromException<IEnumerable<EventType>>(new TimeoutException()));
    }
}
