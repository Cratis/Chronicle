// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

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
    Queue<Task> _tasks = new();
    int _threadsQueuedOrRunning;

    /// <summary>
    /// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the
    /// specified degree of parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism provided by this scheduler.</param>
    public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <summary>
    /// Gets the maximum concurrency level supported by this scheduler.
    /// </summary>
    public sealed override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

    /// <summary>
    /// Check whether or not a specific task is scheduled or running..
    /// </summary>
    /// <param name="task">Task to check.</param>
    /// <returns>True if the task is scheduled or running, false if not.</returns>
    public bool HasTask(Task task) => _tasks.Any(_ => _ == task);

    /// <inheritdoc/>
    protected sealed override IEnumerable<Task> GetScheduledTasks()
    {
        lock (_tasks)
        {
            return _tasks.ToArray();
        }
    }

    /// <inheritdoc/>
    protected sealed override void QueueTask(Task task)
    {
        lock (_tasks)
        {
            _tasks.Enqueue(task);
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
            var newTasks = _tasks.Where(t => t != task);
            var success = newTasks.Count() < _tasks.Count;
            _tasks = new(newTasks);
            return success;
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
                            _tasks.TryDequeue(out item);
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
