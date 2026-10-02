// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances_during_a_replay.given;

/// <summary>
/// A read model holding one instance, being rebuilt by a replay through another sink that has already written
/// a different state for that instance and an instance the read model does not have yet.
/// </summary>
public class an_observed_read_model_being_replayed : for_Sink.given.two_sinks_for_one_read_model
{
    protected static readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);
    protected readonly List<int[]> _pages = [];
    protected IDisposable _subscription;

    readonly Lock _lock = new();
    readonly List<(Func<int[], bool> Condition, TaskCompletionSource<int[]> Completion)> _waiters = [];
    Exception? _observationError;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _otherSink.BeginReplay(ReplayContext());
        await _otherSink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        await _otherSink.ApplyChanges(new Key("counter-2", ArrayIndexers.NoIndexers), ChangesetSettingCountTo(5), 43UL);
    }

    void Destroy() => _subscription?.Dispose();

    /// <summary>
    /// Starts observing the read model through the sink queries use.
    /// </summary>
    protected void Observe() => _subscription = _sink.ObserveInstances().Subscribe(OnPage, OnError);

    /// <summary>
    /// Waits for the first page whose counts satisfy a condition.
    /// </summary>
    /// <param name="condition">The condition the counts on the page must satisfy.</param>
    /// <returns>The counts on the page, or null if no such page arrived in time.</returns>
    protected async Task<int[]?> PageWhere(Func<int[], bool> condition)
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
            return await completion.Task.WaitAsync(_timeout);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

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
