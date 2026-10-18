// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.given;

public class a_scheduler : Specification
{
    protected static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);
    protected readonly ConcurrentQueue<string> _log = new();
    protected readonly ConcurrentDictionary<string, TaskCompletionSource> _started = new();
    protected readonly ConcurrentDictionary<string, TaskCompletionSource> _gates = new();
    PartitionedDeliveryScheduler _scheduler;
    protected int _maxRunning;
    int _running;

    protected virtual int MaxConcurrentPartitions => 2;

    void Establish() => _scheduler = new(MaxConcurrentPartitions);

    protected Task Schedule(string partition, string name) => _scheduler.Schedule(partition, () => Work(name));

    protected Task ScheduleBarrier(string name) => _scheduler.ScheduleBarrier(() => Work(name));

    protected void Stop() => _scheduler.Stop();

    protected Task Started(string name) => StartedFor(name).Task.WaitAsync(_deadline);

    protected void Release(string name) => GateFor(name).TrySetResult();

    protected void Fail(string name) => GateFor(name).TrySetException(new WorkFailed(name));

    protected int IndexOf(string entry) => Array.IndexOf(_log.ToArray(), entry);

    async Task Work(string name)
    {
        var running = Interlocked.Increment(ref _running);
        InterlockedMax(ref _maxRunning, running);
        _log.Enqueue($"{name}:start");
        StartedFor(name).TrySetResult();
        try
        {
            await GateFor(name).Task;
        }
        finally
        {
            _log.Enqueue($"{name}:end");
            Interlocked.Decrement(ref _running);
        }
    }

    TaskCompletionSource StartedFor(string name) => _started.GetOrAdd(name, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    TaskCompletionSource GateFor(string name) => _gates.GetOrAdd(name, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    static void InterlockedMax(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var observed = Interlocked.CompareExchange(ref target, value, current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }

    public class WorkFailed(string name) : Exception($"Work '{name}' failed");
}
