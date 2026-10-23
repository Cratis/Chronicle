// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_scheduling_from_a_caller_with_ambient_state;

public class and_the_work_reads_the_ambient_state : Specification
{
    static readonly AsyncLocal<string?> _ambient = new();
    PartitionedDeliveryScheduler _scheduler;
    string? _seenByWork;
    string? _seenByBarrier;

    void Establish() => _scheduler = new(2);

    async Task Because()
    {
        Task work;
        Task barrier;
        _ambient.Value = "caller";
        try
        {
            work = _scheduler.Schedule("partition", () =>
            {
                _seenByWork = _ambient.Value;
                return Task.CompletedTask;
            });
            barrier = _scheduler.ScheduleBarrier(() =>
            {
                _seenByBarrier = _ambient.Value;
                return Task.CompletedTask;
            });
        }
        finally
        {
            _ambient.Value = null;
        }

        await Task.WhenAll(work, barrier).WaitAsync(TimeSpan.FromSeconds(10));
    }

    void Destroy() => _scheduler.Dispose();

    [Fact] void should_not_flow_the_callers_state_into_partition_work() => _seenByWork.ShouldBeNull();
    [Fact] void should_not_flow_the_callers_state_into_a_barrier() => _seenByBarrier.ShouldBeNull();
}
