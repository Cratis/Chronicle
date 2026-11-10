// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// An observer taking pages spanning partitions, caught up by a sweep whose job enumerated only <see cref="RegisteredPartition"/>.
/// Events appended to other partitions after that enumeration are still read by the sweep, which runs to the end of the sequence.
/// </summary>
public class an_observer_with_a_page_subscriber_catching_up : an_observer_with_a_page_subscriber
{
    protected static readonly Key RegisteredPartition = "bare";
    protected static readonly Key PartitionAppendedAfterEnumeration = "scoped";
    protected JobId _job;

    async Task Establish()
    {
        _job = JobId.New();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(12UL));
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_job)));
        await _observer.CatchUp();
        await _observer.RegisterCatchingUpPartitions([RegisteredPartition]);
        _subscriber.ClearReceivedCalls();
    }
}
