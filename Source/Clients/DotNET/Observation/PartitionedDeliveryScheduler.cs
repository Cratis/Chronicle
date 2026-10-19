// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Runs deliveries for one observer: batches for the same partition run in arrival order, while batches for
/// different partitions run concurrently up to a bound. Barriers wait for everything in flight and hold back
/// everything scheduled after them.
/// </summary>
internal sealed class PartitionedDeliveryScheduler : IDisposable
{
    const string SerialKey = "";

    readonly SemaphoreSlim _slots;
    readonly Dictionary<string, Task> _tails = [];
#if NET8_0
    readonly object _lock = new();
#else
    readonly Lock _lock = new();
#endif
    Task _barrier = Task.CompletedTask;
    volatile bool _stopped;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartitionedDeliveryScheduler"/> class.
    /// </summary>
    /// <param name="maxConcurrentPartitions">The maximum number of partitions handled at the same time. One is serial.</param>
    public PartitionedDeliveryScheduler(int maxConcurrentPartitions)
    {
        InvalidMaxConcurrentPartitions.ThrowIfInvalid(maxConcurrentPartitions);
        MaxConcurrentPartitions = maxConcurrentPartitions;
        _slots = new(maxConcurrentPartitions, maxConcurrentPartitions);
    }

    /// <summary>
    /// Gets the default bound: the processor count, capped at eight.
    /// </summary>
    public static int DefaultMaxConcurrentPartitions => Math.Min(Environment.ProcessorCount, 8);

    /// <summary>
    /// Gets the maximum number of partitions handled at the same time.
    /// </summary>
    public int MaxConcurrentPartitions { get; }

    /// <summary>
    /// Schedules work for a partition. It runs after earlier work for the same partition and after any barrier.
    /// </summary>
    /// <param name="partition">The partition the work belongs to.</param>
    /// <param name="work">The work to run.</param>
    /// <returns>A task that completes when the work has run or was skipped.</returns>
    public Task Schedule(string partition, Func<Task> work)
    {
        lock (_lock)
        {
            var key = MaxConcurrentPartitions == 1 ? SerialKey : partition;
            var previous = _tails.TryGetValue(key, out var tail) ? tail : _barrier;
            var task = Task.Run(() => Run(previous, work, useSlot: true));
            _tails[key] = task;
            _ = task.ContinueWith(
                _ =>
                {
                    lock (_lock)
                    {
                        if (_tails.TryGetValue(key, out var current) && current == task)
                        {
                            _tails.Remove(key);
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return task;
        }
    }

    /// <summary>
    /// Schedules work that waits for all work in flight and holds back all work scheduled after it.
    /// </summary>
    /// <param name="work">The work to run.</param>
    /// <returns>A task that completes when the work has run or was skipped.</returns>
    public Task ScheduleBarrier(Func<Task> work)
    {
        lock (_lock)
        {
            var drain = Task.WhenAll(_tails.Values.Append(_barrier).ToArray());
            var task = Task.Run(() => Run(drain, work, useSlot: false));
            _barrier = task;
            _tails.Clear();
            return task;
        }
    }

    /// <summary>
    /// Stops the scheduler. Work that has not started is skipped; running work finishes.
    /// </summary>
    public void Stop() => _stopped = true;

    /// <summary>
    /// Waits for all work that is scheduled or running to finish. Failures are not rethrown: draining never throws.
    /// </summary>
    /// <returns>A task that completes when everything scheduled so far has finished.</returns>
    public async Task Drain()
    {
        Task[] pending;
        lock (_lock)
        {
            pending = [.. _tails.Values.Append(_barrier)];
        }

        foreach (var task in pending)
        {
            await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    /// <summary>
    /// Stops the scheduler, waits for running work to finish and releases its resources.
    /// </summary>
    /// <returns>A task that completes when the scheduler has drained and been disposed.</returns>
    public async Task DrainAndDispose()
    {
        Stop();
        await Drain();
        Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
        _slots.Dispose();
    }

    async Task Run(Task previous, Func<Task> work, bool useSlot)
    {
        await previous.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (_stopped)
        {
            return;
        }

        if (useSlot)
        {
            await _slots.WaitAsync();
        }

        try
        {
            if (_stopped)
            {
                return;
            }

            await work();
        }
        catch
        {
            _stopped = true;
            throw;
        }
        finally
        {
            if (useSlot)
            {
                ReleaseSlot();
            }
        }
    }

    void ReleaseSlot()
    {
        try
        {
            _slots.Release();
        }
        catch (ObjectDisposedException) when (_stopped)
        {
            // Disposed after stopping while this work was still finishing; there is nobody left to wait for the slot.
        }
    }
}
