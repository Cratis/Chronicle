// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_drops_parked_sequence_checkpoints : given.an_observer_with_subscription
{
    async Task Establish()
    {
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
    }

    async Task Because() => await _observer.Replay();

    [Fact] void should_remove_the_checkpoints_of_the_other_sequences() => _eventStoreNamespaceStorage.ObserverSequenceCheckpoints.Received(1).RemoveAllFor(_observerId);
}
