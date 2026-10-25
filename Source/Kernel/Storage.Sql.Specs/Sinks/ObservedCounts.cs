// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Records observed read model counts and waits for pages matching a condition.
/// </summary>
public sealed class ObservedCounts : IDisposable
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(30);

    readonly Lock _lock = new();
    readonly List<int[]> _pages = [];
    readonly List<(Func<int[], bool> Condition, TaskCompletionSource<int[]> Completion)> _waiters = [];
    IDisposable? _subscription;
    Exception? _observationError;

    /// <summary>
    /// Starts observing the read model through the sink queries use.
    /// </summary>
    /// <param name="sink">The sink to observe.</param>
    public void Observe(ISink sink)
    {
        // Start polling on the thread pool, not xUnit's bounded context, matching production gRPC callers with no SynchronizationContext.
        _subscription = sink.ObserveInstances().SubscribeOn(TaskPoolScheduler.Default).Subscribe(OnPage, OnError);
    }

    /// <summary>
    /// Waits for the first page whose counts satisfy a condition.
    /// </summary>
    /// <param name="condition">The condition the counts on the page must satisfy.</param>
    /// <returns>The counts on the page, or null if no such page arrived in time.</returns>
    public async Task<int[]?> PageWhere(Func<int[], bool> condition)
    {
        var completion = new TaskCompletionSource<int[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
        {
            var existing = _pages.FirstOrDefault(condition);
            if (existing is not null)
            {
                return existing;
            }

            if (_observationError is not null)
            {
                completion.TrySetException(_observationError);
            }
            else
            {
                _waiters.Add((condition, completion));
            }
        }

        try
        {
            return await completion.Task.WaitAsync(_deadline, TimeProvider.System);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _subscription?.Dispose();

    static int CountOf(ExpandoObject instance) =>
        Convert.ToInt32(((IDictionary<string, object?>)instance)["count"], CultureInfo.InvariantCulture);

    void OnError(Exception error)
    {
        lock (_lock)
        {
            _observationError = error;
            foreach (var waiter in _waiters)
            {
                waiter.Completion.TrySetException(error);
            }
            _waiters.Clear();
        }
    }

    void OnPage(IEnumerable<ExpandoObject> page)
    {
        var counts = page.Select(CountOf).ToArray();
        lock (_lock)
        {
            _pages.Add(counts);
            foreach (var waiter in _waiters.Where(_ => _.Condition(counts)).ToArray())
            {
                waiter.Completion.TrySetResult(counts);
                _waiters.Remove(waiter);
            }
        }
    }
}
