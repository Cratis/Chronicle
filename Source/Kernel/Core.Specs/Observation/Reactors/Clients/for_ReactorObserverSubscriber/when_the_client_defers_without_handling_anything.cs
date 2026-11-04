// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_the_client_defers_without_handling_anything : given.a_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        var delivery = _subscriber.OnNext("partition", [AppendedEvent.EmptyWithEventSequenceNumber(7), AppendedEvent.EmptyWithEventSequenceNumber(8)], _context);
        _completion.SetResult(ObserverSubscriberResultDeferrals.Deferred(EventSequenceNumber.Unavailable, TimeSpan.FromSeconds(30), _clock.GetUtcNow()));
        _result = await delivery;
    }

    [Fact] void should_report_the_partition_as_deferred() => _result.State.ShouldEqual(ObserverSubscriberState.Deferred);
    [Fact] void should_resume_from_the_first_delivered_event() => _result.Deferral!.ResumeFrom.ShouldEqual((EventSequenceNumber)7UL);
    [Fact] void should_say_the_observer_asked_for_it() => _result.Deferral!.Reason.ShouldEqual(PartitionDeferralReason.RequestedByObserver);
}
