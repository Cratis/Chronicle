// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A resumed catch-up job delivers events of partitions the observer does not know it owns yet. Until they are registered, no live event may be handed to the subscriber beside it.
/// </summary>
public class when_a_catch_up_job_resumes : given.an_observer_with_subscription_for_specific_event_type
{
    bool _isPreparingCatchup;

    void Establish() => _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 40UL };

    async Task Because()
    {
        await _observer.ResumingCatchUp(JobId.New());
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);
    }

    [Fact] void should_be_preparing_catch_up() => _isPreparingCatchup.ShouldBeTrue();
    [Fact] void should_not_forward_live_events_to_the_subscriber() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
