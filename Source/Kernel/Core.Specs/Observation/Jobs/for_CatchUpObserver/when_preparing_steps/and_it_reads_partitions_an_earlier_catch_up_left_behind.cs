// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

/// <summary>
/// An earlier catch-up got every partition to 12 but left events behind for two of them. This catch-up reads only
/// those two, each from where its own step got to and no further than 12 - what lies past it is routing's, as for every
/// other partition - and keeps both held back while it does (#4583).
/// </summary>
public class and_it_reads_partitions_an_earlier_catch_up_left_behind : given.a_catch_up_observer_job
{
    static readonly Key _first = (Key)"partition-1";
    static readonly Key _second = (Key)"partition-2";
    static readonly EventSequenceNumber _caughtUpTo = 12UL;

    IEnumerable<HandleEventsForPartitionArguments> _steps = [];

    void Establish()
    {
        _request = _request with
        {
            ObserverType = ObserverType.Reactor,
            PartitionsLeftBehind = [new(_first, 11UL), new(_second, 7UL)],
            ToEventSequenceNumber = _caughtUpTo
        };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_first, _second, (Key)"partition-3"));
    }

    async Task Because()
    {
        await StartAndLetTheStepsCome();
        _steps = _job.PreparedSteps!.Select(step => (HandleEventsForPartitionArguments)step.Request).ToArray();
    }

    [Fact] void should_prepare_a_step_for_each_partition_left_behind_only() => _steps.Select(_ => _.Partition).ShouldContainOnly(_first, _second);
    [Fact] void should_read_each_from_where_its_own_step_got_to() => _steps.Select(_ => (_.Partition, _.StartEventSequenceNumber)).ShouldContainOnly((_first, (EventSequenceNumber)11UL), (_second, (EventSequenceNumber)7UL));
    [Fact] void should_read_no_further_than_where_the_earlier_catch_up_got_every_other_partition() => _steps.All(_ => _.EndEventSequenceNumber == _caughtUpTo).ShouldBeTrue();
    [Fact] void should_not_have_the_steps_look_past_that() => _steps.Any(_ => _.ConcludesPartitionCatchUp).ShouldBeFalse();
    [Fact] async Task should_keep_the_partitions_held_back() => await _observer.Received(1).RegisterCatchingUpPartitions(Arg.Is<IEnumerable<Key>>(keys => keys.SequenceEqual(new[] { _first, _second })));
}
