// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_quarantined_observer_with_remaining_events : a_quarantined_observer
{
    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)50UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(43UL, Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)43UL);
    }

    protected void ShouldCatchUpFromRecordedPosition() => _jobsManager.Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(_ =>
            _.ObserverKey == _observerKey && _.FromEventSequenceNumber == (EventSequenceNumber)43UL));
}
