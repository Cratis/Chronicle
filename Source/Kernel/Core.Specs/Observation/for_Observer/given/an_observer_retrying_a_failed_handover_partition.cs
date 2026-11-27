// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_retrying_a_failed_handover_partition : an_observer_with_a_handover_log
{
    protected virtual bool DeliversPartitionPages => false;
    protected bool FailedDuringSuccessorRead { get; private set; }

    protected async Task RunHandoverAndRetry()
    {
        var first = Append("P", 12UL);
        await _observer.PartitionFailed("P", first.Context.SequenceNumber, ["unavailable"], string.Empty);
        var episode = _failedPartitionsStorage.State.Partitions.Single().Id;
        Append("P", 18UL);
        Append("Q", 19UL);
        if (DeliversPartitionPages)
        {
            var request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, 12UL, [event_type]);
            var steps = await handover_successor_reader.Prepare(request, _observer, ["P", "Q"]);
            var subscriber = Substitute.For<IObserverPartitionPageSubscriber>();
            foreach (var step in steps)
            {
                if (step.Request is HandleEventsForObserverArguments arguments)
                {
                    await handover_successor_reader.WalkPages(arguments, _observer, _eventSequenceStorage, subscriber);
                }
            }
        }
        await _observer.CaughtUp(JobId.NotSet, 19UL, 19UL);
        await _silo.RunCatchUpContinuation();
        FailedDuringSuccessorRead = _failedPartitionsStorage.State.IsFailed((Key)"P");
        await DeliverRange(12UL, 18UL, [event_type], "P");
        Append("P", 20UL);
        await _observer.FailedPartitionRecovered("P", 18UL, 18UL, episode);
        await _silo.RunCatchUpContinuation();
        await _observer.Handle("Q", [Append("Q", 21UL)]);
    }
}
