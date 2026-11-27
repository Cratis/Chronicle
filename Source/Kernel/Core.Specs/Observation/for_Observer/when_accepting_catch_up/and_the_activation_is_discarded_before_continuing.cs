// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_the_activation_is_discarded_before_continuing : given.an_observer_accepting_a_catch_up_handover
{
    ObserverState _restored;

    async Task Establish()
    {
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
    }

    async Task Because()
    {
        await Crash();
        _observer.SetSubscription(new ObserverSubscription(_observerId, _observerKey, [EventType.Unknown], typeof(NullObserverSubscriber), SiloAddress.Zero, null));
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
        _restored = await _observer.GetState();
    }

    [Fact] void should_keep_the_accepted_cursor() => _restored.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
}
