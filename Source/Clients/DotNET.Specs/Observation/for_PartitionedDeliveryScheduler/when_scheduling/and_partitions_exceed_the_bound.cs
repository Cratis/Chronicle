// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_scheduling;

public class and_partitions_exceed_the_bound : given.a_scheduler
{
    static readonly string[] _partitions = ["a", "b", "c"];
    int _startedAtTheBound;

    async Task Because()
    {
        var work = _partitions.Select(partition => Schedule(partition, partition)).ToArray();
        var pending = _partitions.Select(Started).ToList();
        for (var started = 0; started < MaxConcurrentPartitions; started++)
        {
            var next = await Task.WhenAny(pending);
            await next;
            pending.Remove(next);
        }

        _startedAtTheBound = _started.Values.Count(_ => _.Task.IsCompleted);
        foreach (var partition in _partitions)
        {
            Release(partition);
        }

        await Task.WhenAll(work).WaitAsync(_deadline);
    }

    [Fact] void should_run_as_many_partitions_as_the_bound_allows() => _startedAtTheBound.ShouldEqual(MaxConcurrentPartitions);
    [Fact] void should_never_run_more_partitions_than_the_bound() => _maxRunning.ShouldEqual(MaxConcurrentPartitions);
    [Fact] void should_run_every_partition() => _partitions.All(partition => IndexOf($"{partition}:end") >= 0).ShouldBeTrue();
}
