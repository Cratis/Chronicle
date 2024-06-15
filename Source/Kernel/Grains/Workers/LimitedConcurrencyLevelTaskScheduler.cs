// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Workers;

/// <summary>
/// Represents a task scheduler that ensures a maximum concurrency level while
/// running on top of the ThreadPool.
/// </summary>
/// <remarks>
/// Taken from https://msdn.microsoft.com/en-us/library/ee789351(v=vs.100).aspx.
/// </remarks>
public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
{
    [ThreadStatic]
    static bool _currentThreadIsProcessingItems;

    readonly int _maxDegreeOfParallelism;
    readonly LinkedList<Task> _tasks = new();
    int _threadsQueuedOrRunning;

    /// <summary>
    /// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the
    /// specified degree of parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism provided by this scheduler.</param>
    public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1) throw new InvalidMaximumConcurrencyLevel(maxDegreeOfParallelism);
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <summary>
    /// Gets the maximum concurrency level supported by this scheduler.
    /// </summary>
    public sealed override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

    /// <inheritdoc/>
    protected sealed override IEnumerable<Task> GetScheduledTasks()
    {
        var lockTaken = false;
        try
        {
            Monitor.TryEnter(_tasks, ref lockTaken);
            if (lockTaken) return [.. _tasks];

            throw new NotSupportedException();
        }
        finally
        {
            if (lockTaken) Monitor.Exit(_tasks);
        }
    }

    /// <inheritdoc/>
    protected sealed override void QueueTask(Task task)
    {
        lock (_tasks)
        {
            _tasks.AddLast(task);
            if (_threadsQueuedOrRunning < _maxDegreeOfParallelism)
            {
                NotifyThreadPoolOfPendingWork();
            }
        }
    }

    /// <inheritdoc/>
    protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // If this thread isn't already processing a task, we don't support inlining
        if (!_currentThreadIsProcessingItems)
        {
            return false;
        }

        // If the task was previously queued, remove it from the queue
        if (taskWasPreviouslyQueued)
        {
            TryDequeue(task);
        }

        // Try to run the task.
        return TryExecuteTask(task);
    }

    /// <inheritdoc/>
    protected sealed override bool TryDequeue(Task task)
    {
        lock (_tasks)
        {
            return _tasks.Remove(task);
        }
    }

    void NotifyThreadPoolOfPendingWork()
    {
        _threadsQueuedOrRunning++;
        ThreadPool.UnsafeQueueUserWorkItem(
            _ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while (true)
                    {
                        Task? item;
                        lock (_tasks)
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if (_tasks.Count == 0)
                            {
                                _threadsQueuedOrRunning--;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First?.Value;
                            if (item is not null)
                            {
                                _tasks.RemoveFirst();
                            }
                        }

                        // Execute the task we pulled out of the queue
                        if (item is not null)
                        {
                            TryExecuteTask(item);
                        }
                    }
                }
                finally
                {
                    _currentThreadIsProcessingItems = false;
                }
            },
            null);
    }
}
