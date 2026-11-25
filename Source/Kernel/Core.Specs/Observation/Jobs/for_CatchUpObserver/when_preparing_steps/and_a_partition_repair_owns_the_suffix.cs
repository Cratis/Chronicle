// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_a_partition_repair_owns_the_suffix : given.a_catch_up_observer_job
{
    void Establish()
    {
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys("A", "B"));
        _observer.GetState().Returns(ObserverState.Empty with { PendingCatchUpRepairs = [new("B", 10UL, 13UL)], NextEventSequenceNumber = 13UL });
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_leave_the_repair_out_of_the_wide_job_holds() => _observer.Received().RegisterCatchingUpPartitions(Arg.Is<IEnumerable<Key>>(_ => _.SequenceEqual(new Key[] { "A" })));
    [Fact] void should_persist_the_partition_exclusion_in_the_global_step() => ((HandleEventsForObserverArguments)_job.PreparedSteps!.Single().Request).ExcludedPartitions.ShouldContainOnly((Key)"B");
}
