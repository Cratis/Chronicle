// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_partitions_are_registered_after_barrier_admission : given.a_catch_up_observer_job
{
    void Establish()
    {
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys("A", "B"));
        _observer.GetState().Returns(ObserverState.Empty with { PendingCatchUpRepairs = [new("B", 10UL, 13UL)], NextEventSequenceNumber = 13UL });
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_cover_both_partitions_after_barrier_admission() => _observer.Received().RegisterCatchingUpPartitions(Arg.Is<IEnumerable<Key>>(_ => _.SequenceEqual(new Key[] { "A", "B" })));
    [Fact] void should_only_skip_failed_partitions() => ((HandleEventsForObserverArguments)_job.PreparedSteps!.Single().Request).SkipFailedPartitions.ShouldBeTrue();
}
