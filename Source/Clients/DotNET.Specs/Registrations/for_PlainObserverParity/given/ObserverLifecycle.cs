// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Registrations.for_PlainObserverParity.given;

public abstract class ObserverLifecycle : ICanBeNotifiedWhenReplay, ICanBeNotifiedWhenPartitionReplayed, IAsyncDisposable
{
    readonly Ledger _ledger;

    protected ObserverLifecycle(Ledger ledger)
    {
        _ledger = ledger;
        Record("created");
    }

    public Task BeginReplay() => Notify("begin");
    public Task EndReplay() => Notify("end");
    public Task BeginReplayPartition(Partition partition) => Notify($"begin:{partition.Value}");
    public Task EndReplayPartition(Partition partition) => Notify($"end:{partition.Value}");

    public ValueTask DisposeAsync()
    {
        Record("disposed");
        return ValueTask.CompletedTask;
    }

    protected void Record(string action) => _ledger.Record(GetType(), action);

    Task Notify(string action)
    {
        Record(action);
        return Task.CompletedTask;
    }

    public class Ledger
    {
        readonly object _lock = new();
        readonly Dictionary<Type, List<string>> _actions = [];
        readonly Dictionary<(Type Type, int Count), TaskCompletionSource> _disposals = [];

        public void Record(Type type, string action)
        {
            lock (_lock)
            {
                if (!_actions.TryGetValue(type, out var actions)) _actions[type] = actions = [];
                actions.Add(action);
                if (action == "disposed") Signal(type, actions.Count(_ => _ == "disposed")).TrySetResult();
            }
        }

        public string[] ActionsFor(Type type)
        {
            lock (_lock) return _actions[type].ToArray();
        }

        public Task WaitForDisposals(Type type, int count)
        {
            lock (_lock) return Signal(type, count).Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        TaskCompletionSource Signal(Type type, int count)
        {
            if (!_disposals.TryGetValue((type, count), out var completion))
            {
                _disposals[(type, count)] = completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            return completion;
        }
    }
}
